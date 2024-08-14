using System;
using Sandbox.Diagnostics;

namespace Voxel;

public partial class ChunkMesh
{
	const int ChunkStepY = 1;
	const int ChunkStepX = Constants.MaxChunkAmount;
	const int ChunkStepZ = Constants.MaxChunkAmountSquared;

	const int AccessStepY = 1;
	const int AccessStepX = Constants.ChunkSize;
	const int AccessStepZ = Constants.ChunkSizeSquared;

	public VoxelModel Model;
	public Chunk Chunk;

	Chunk ChunkXN;
	Chunk ChunkXP;
	Chunk ChunkYN;
	Chunk ChunkYP;
	Chunk ChunkZN;
	Chunk ChunkZP;

	int ChunkPosX => Chunk.ChunkPosX;
	int ChunkPosY => Chunk.ChunkPosY;
	int ChunkPosZ => Chunk.ChunkPosZ;

	public Vector3 WorldPos;
	public Transform Transform => new( new Vector3( WorldPos.z, WorldPos.x, WorldPos.y ) * Constants.VoxelSize );

	static readonly uint[] Buffer = new uint[Constants.ChunkSizeCubed * 6 * 6];
	static readonly Vector3[] Vertices = new Vector3[Constants.ChunkSizeCubed * 6 * 4];
	static readonly MeshVisiter meshVisiter = new();
	int BufferWrite = 0;

	public SceneObject SceneObject { get; private set; }
	public PhysicsBody PhysicsBody { get; private set; }
	public Mesh Mesh { get; private set; }
	public PhysicsShape PhysicsShape { get; private set; }

	private static readonly VertexAttribute[] Layout =
	{
		new( VertexAttributeType.TexCoord, VertexAttributeFormat.UInt32, 1, 10 )
	};

	public ChunkMesh( VoxelModel model, Chunk chunk )
	{
		Assert.True( chunk != null );
		Assert.True( chunk.Allocated );

		Model = model;
		Chunk = chunk;

		WorldPos = new( ChunkPosX * Constants.ChunkSize, ChunkPosY * Constants.ChunkSize, ChunkPosZ * Constants.ChunkSize );

		GetNeighbourReferences();
	}

	public void GetNeighbourReferences()
	{
		var chunkAccess = Model.GetAccessLocal( ChunkPosX, ChunkPosY, ChunkPosZ );
		var exteriorChunk = Model.ShouldMeshExterior ? VoxelModel.EmptyChunk : VoxelModel.FullChunk;

		ChunkXN = ChunkPosX > 0 ? Model.Chunks[chunkAccess - ChunkStepX] : exteriorChunk;
		ChunkXP = ChunkPosX < Model.ChunkAmountXM1 ? Model.Chunks[chunkAccess + ChunkStepX] : exteriorChunk;

		ChunkYN = ChunkPosY > 0 ? Model.Chunks[chunkAccess - ChunkStepY] : exteriorChunk;
		ChunkYP = ChunkPosY < Model.ChunkAmountYM1 ? Model.Chunks[chunkAccess + ChunkStepY] : exteriorChunk;

		ChunkZN = ChunkPosZ > 0 ? Model.Chunks[chunkAccess - ChunkStepZ] : exteriorChunk;
		ChunkZP = ChunkPosZ < Model.ChunkAmountZM1 ? Model.Chunks[chunkAccess + ChunkStepZ] : exteriorChunk;
	}

	public void PreMeshing()
	{
		Assert.True( Chunk.Voxels != null );
		Assert.True( Chunk.Dirty );

		Chunk.UnsetDirty();
	}

	public void PostMeshing( SceneWorld scene, PhysicsWorld world, Transform transform )
	{
		var size = BufferWrite;
		if ( size > 0 )
		{
			var indices = new int[size];
			for ( int i = 0; i < size; i++ )
				indices[i] = i;

			if ( !SceneObject.IsValid() )
			{
				var modelBuilder = new ModelBuilder();
				var material = Material.Load( "materials/voxel.vmat" );
				Mesh = new Mesh( material );

				var boundsMin = Vector3.Zero;
				var boundsMax = boundsMin + (Constants.ChunkSize * Constants.VoxelSize);
				Mesh.Bounds = new BBox( boundsMin, boundsMax );

				Mesh.CreateVertexBuffer( size, Layout, Buffer.AsSpan() );
				modelBuilder.AddMesh( Mesh );
				var model = modelBuilder.Create();

				SceneObject = new SceneObject( scene, model, transform.ToWorld( Transform ) );
				SceneObject.Flags.CastShadows = true;
				SceneObject.Flags.IsOpaque = true;
				SceneObject.Flags.IsTranslucent = false;
				SceneObject.Attributes.Set( "VoxelSize", Constants.VoxelSize );
				SceneObject.Attributes.Set( "ColorPalette", Model.PaletteBuffer );

				PhysicsBody = new PhysicsBody( world );
				PhysicsBody.Transform = transform.ToWorld( Transform );

				PhysicsShape = PhysicsBody.AddMeshShape( Vertices, indices );
				PhysicsShape.Tags.Add( "voxel" );
			}
			else
			{
				Mesh.SetVertexBufferSize( size );
				Mesh.SetVertexBufferData( Buffer.AsSpan().Slice( 0, size ) );

				PhysicsShape.UpdateMesh( Vertices, indices );
			}
		}
		else
		{
			SceneObject?.Delete();
			SceneObject = null;

			Mesh = null;

			PhysicsBody?.Remove();
			PhysicsBody = null;
			PhysicsShape = null;
		}
	}

	public void Destroy()
	{
		if ( SceneObject.IsValid() )
		{
			SceneObject.Delete();
			SceneObject = null;
		}

		if ( PhysicsBody.IsValid() )
		{
			PhysicsBody.Remove();
			PhysicsBody = null;
		}
	}

	void WriteVertex( int i, int j, int k, byte normal, byte kind )
	{
		byte brightness = 7;
		byte texture = (byte)(kind - 1);
		uint shared = (uint)((texture & 255) << 24 | (normal & 7) << 21 | (brightness & 7) << 18);
		Vertices[BufferWrite] = new Vector3( k, i, j ) * Constants.VoxelSize;
		Buffer[BufferWrite++] = shared | ((uint)j & 63) << 12 | ((uint)i & 63) << 6 | ((uint)k & 63);
	}

	public void GenerateMesh()
	{
		Assert.True( Chunk.Allocated );
		Assert.True( !Chunk.Fake );

		BufferWrite = 0;
		meshVisiter.Reset();

		var zAccess = 0;
		var maxYPointer = 0;
		var minYPointer = 0;

		for ( int k = 0; k < Constants.ChunkSize; k++ )
		{
			var xAccess = 0;

			for ( int i = 0; i < Constants.ChunkSize; i++ )
			{
				int j = Chunk.MinAltitude[minYPointer++];
				int maxJ = Chunk.MaxAltitude[maxYPointer++];

				var access = zAccess + xAccess + j;
				var voxel = access;

				for ( ; j <= maxJ; j++, access++, voxel++ )
				{
					if ( Chunk.Voxels[voxel] > 0 )
					{
						CreateRuns( voxel, i, j, k, access, xAccess, zAccess );
					}
				}

				xAccess += Constants.ChunkSize;
			}

			zAccess += Constants.ChunkSizeSquared;
		}
	}

	protected void CreateRuns( int voxel, int i, int j, int k, int access, int xAccess, int zAccess )
	{
		Assert.True( meshVisiter != null );

		// Check if we're on the edge of this chunk
		var minX = i == 0;
		var maxX = i == Constants.ChunkSizeM1;

		var minZ = k == 0;
		var maxZ = k == Constants.ChunkSizeM1;

		var minY = j == 0;
		var maxY = j == Constants.ChunkSizeM1;

		// Precalculate mesh visiters for each face
		var visitXN = access;
		var visitXP = access;
		var visitYN = access;
		var visitYP = access;
		var visitZN = access;
		var visitZP = access;

		// Precalculate
		var data = Chunk.Voxels;
		var index = data[voxel];
		var comparison = meshVisiter.Comparison;
		byte textureID = index;
		var i1 = i + 1;
		var j1 = j + 1;

		// 'a' refers to the first axis we combine faces along
		// 'b' refers to the second axis we combine faces along
		//      e.g. for Y+ faces, we merge along the X axis, then along the A axis
		//           for X- faces, we merge up along the Y axis, then along the Z axis
		int end_a;
		int length_b;

		// Left (X-)
		if ( meshVisiter.visitXN[visitXN] != comparison && DrawFaceXN( j, voxel, minX, zAccess ) )
		{
			var originalXN = visitXN;

			// Remember we've meshed this face
			meshVisiter.visitXN[visitXN] = comparison;
			visitXN += AccessStepY;

			// Combine faces upwards along the Y axis
			var voxelPointer = access + AccessStepY;
			var yAccess = j1;

			for ( end_a = j1; end_a < Constants.ChunkSize; end_a++ )
			{
				if ( data[voxelPointer] != index ||                           // It's a different kind of voxel
					!DrawFaceXN( yAccess, voxelPointer, minX, zAccess ) ||          // This voxel face is covered by another voxel
					meshVisiter.visitXN[visitXN] == comparison )                    // We've already meshed this voxel face
					break;

				// Step upwards
				voxelPointer++;
				yAccess++;

				// Remember we've meshed this face
				meshVisiter.visitXN[visitXN] = comparison;
				visitXN += AccessStepY;
			}

			// Calculate how many voxels we combined along the Y axis
			var length_a = end_a - j1 + 1;

			// Combine faces along the Z axis
			length_b = 1;

			var max_length_b = Constants.ChunkSize - k;
			var netZAccess = zAccess;

			for ( int g = 1; g < max_length_b; g++ )
			{
				// Go back to where we started, then move g units along the Z axis
				voxelPointer = access;
				voxelPointer += AccessStepZ * g;
				yAccess = j;

				// Check if the entire row next to us is also the same index and not covered by another block
				bool adjacentRowIsIdentical = true;

				for ( var test_a = j; test_a < end_a; test_a++ )
				{
					// No need to check the meshVisiter here as we're combining on this axis for the first time
					if ( data[voxelPointer] != index || !DrawFaceXN( yAccess, voxelPointer, minX, netZAccess ) )
					{
						adjacentRowIsIdentical = false;
						break;
					}

					voxelPointer++;
					yAccess++;
				}

				if ( !adjacentRowIsIdentical )
					break;

				// We found a whole row that's valid!
				length_b++;

				// Remember we've meshed these faces
				var tempXN = originalXN;
				tempXN += AccessStepZ * g;

				for ( int h = 0; h < length_a; h++ )
				{
					meshVisiter.visitXN[tempXN] = comparison;
					tempXN += AccessStepY;
				}
			}

			WriteVertex( i, j, k, 2, textureID );
			WriteVertex( i, j, k + length_b, 2, textureID );
			WriteVertex( i, j + length_a, k, 2, textureID );

			WriteVertex( i, j + length_a, k, 2, textureID );
			WriteVertex( i, j, k + length_b, 2, textureID );
			WriteVertex( i, j + length_a, k + length_b, 2, textureID );
		}

		// Right (X+)
		if ( meshVisiter.visitXP[visitXP] != comparison && DrawFaceXP( j, voxel, maxX, zAccess ) )
		{
			var originalXP = visitXP;

			// Remember we've meshed this face
			meshVisiter.visitXP[visitXP] = comparison;
			visitXP += AccessStepY;

			// Combine faces along the Y axis
			var voxelPointer = access + AccessStepY;
			var yAccess = j1;

			for ( end_a = j1; end_a < Constants.ChunkSize; end_a++ )
			{
				if ( data[voxelPointer] != index || !DrawFaceXP( yAccess, voxelPointer, maxX, zAccess ) || meshVisiter.visitXP[visitXP] == comparison )
					break;

				voxelPointer++;
				yAccess++;

				// Remember we've meshed this face
				meshVisiter.visitXP[visitXP] = comparison;
				visitXP += AccessStepY;
			}

			// Calculate how many voxels we combined along the Y axis
			var length_a = end_a - j1 + 1;

			// Combine faces along the Z axis
			length_b = 1;

			var max_length_b = Constants.ChunkSize - k;
			var netZAccess = zAccess;

			for ( int g = 1; g < max_length_b; g++ )
			{
				// Go back to where we started, then move g units on the Z axis
				voxelPointer = access;
				voxelPointer += AccessStepZ * g;
				yAccess = j;

				// Check if the entire row next to us is also the same index and not covered by another block
				bool adjacentRowIsIdentical = true;

				for ( var test_a = j; test_a < end_a; test_a++ )
				{
					// No need to check *yp here as we're combining on this axis for the first time
					if ( data[voxelPointer] != index || !DrawFaceXP( yAccess, voxelPointer, maxX, netZAccess ) )
					{
						adjacentRowIsIdentical = false;
						break;
					}

					voxelPointer++;
					yAccess++;
				}

				if ( !adjacentRowIsIdentical )
					break;

				// We found a whole row that's valid!
				length_b++;

				// Remember we've meshed these faces
				var tempXP = originalXP;
				tempXP += AccessStepZ * g;

				for ( int h = 0; h < length_a; h++ )
				{
					meshVisiter.visitXP[tempXP] = comparison;
					tempXP += AccessStepY;
				}
			}

			WriteVertex( i + 1, j, k, 3, textureID );
			WriteVertex( i + 1, j + length_a, k, 3, textureID );
			WriteVertex( i + 1, j, k + length_b, 3, textureID );

			WriteVertex( i + 1, j, k + length_b, 3, textureID );
			WriteVertex( i + 1, j + length_a, k, 3, textureID );
			WriteVertex( i + 1, j + length_a, k + length_b, 3, textureID );
		}

		// Back (Z-)
		if ( meshVisiter.visitZN[visitZN] != comparison && DrawFaceZN( j, voxel, minZ, xAccess ) )
		{
			var originalZN = visitZN;

			// Remember we've meshed this face
			meshVisiter.visitZN[visitZN] = comparison;
			visitZN += AccessStepY;

			// Combine faces along the Y axis
			var voxelPointer = access + AccessStepY;
			var yAccess = j1;

			for ( end_a = j1; end_a < Constants.ChunkSize; end_a++ )
			{
				if ( data[voxelPointer] != index || !DrawFaceZN( yAccess, voxelPointer, minZ, xAccess ) || meshVisiter.visitZN[visitZN] == comparison )
					break;

				voxelPointer++;
				yAccess++;

				// Remember we've meshed this face
				meshVisiter.visitZN[visitZN] = comparison;
				visitZN += AccessStepY;
			}

			// Calculate how many voxels we combined along the Y axis
			var length_a = end_a - j1 + 1;

			// Combine faces along the X axis
			length_b = 1;

			var max_length_b = Constants.ChunkSize - i;
			var netXAccess = xAccess;

			for ( int g = 1; g < max_length_b; g++ )
			{
				// Go back to where we started, then move g units on the X axis
				voxelPointer = access;
				voxelPointer += AccessStepX * g;
				yAccess = j;

				// Check if the entire row next to us is also the same index and not covered by another block
				bool adjacentRowIsIdentical = true;

				for ( var test_a = j; test_a < end_a; test_a++ )
				{
					// No need to check *yp here as we're combining on this axis for the first time
					if ( data[voxelPointer] != index || !DrawFaceZN( yAccess, voxelPointer, minZ, netXAccess ) )
					{
						adjacentRowIsIdentical = false;
						break;
					}

					voxelPointer++;
					yAccess++;
				}

				if ( !adjacentRowIsIdentical )
					break;

				// We found a whole row that's valid!
				length_b++;

				// Remember we've meshed these faces
				var tempZN = originalZN;
				tempZN += AccessStepX * g;

				for ( int h = 0; h < length_a; h++ )
				{
					meshVisiter.visitZN[tempZN] = comparison;
					tempZN += AccessStepY;
				}
			}

			WriteVertex( i, j, k, 4, textureID );
			WriteVertex( i, j + length_a, k, 4, textureID );
			WriteVertex( i + length_b, j, k, 4, textureID );

			WriteVertex( i + length_b, j, k, 4, textureID );
			WriteVertex( i, j + length_a, k, 4, textureID );
			WriteVertex( i + length_b, j + length_a, k, 4, textureID );
		}

		// Front (Z+)
		if ( meshVisiter.visitZP[visitZP] != comparison && DrawFaceZP( j, voxel, maxZ, xAccess ) )
		{
			var originalZP = visitZP;

			// Remember we've meshed this face
			meshVisiter.visitZP[visitZP] = comparison;
			visitZP += AccessStepY;

			// Combine faces along the Y axis
			var voxelPointer = access + AccessStepY;
			var yAccess = j1;

			for ( end_a = j1; end_a < Constants.ChunkSize; end_a++ )
			{
				if ( data[voxelPointer] != index || !DrawFaceZP( yAccess, voxelPointer, maxZ, xAccess ) || meshVisiter.visitZP[visitZP] == comparison )
					break;

				voxelPointer++;
				yAccess++;

				// Remember we've meshed this face
				meshVisiter.visitZP[visitZP] = comparison;
				visitZP += AccessStepY;
			}

			// Calculate how many voxels we combined along the Y axis
			var length_a = end_a - j1 + 1;

			// Combine faces along the X axis
			length_b = 1;

			var max_length_b = Constants.ChunkSize - i;
			var netXAccess = xAccess;

			for ( int g = 1; g < max_length_b; g++ )
			{
				// Go back to where we started, then move g units on the X axis
				voxelPointer = access;
				voxelPointer += AccessStepX * g;
				yAccess = j;

				// Check if the entire row next to us is also the same index and not covered by another block
				bool adjacentRowIsIdentical = true;

				for ( var test_a = j; test_a < end_a; test_a++ )
				{
					// No need to check *yp here as we're combining on this axis for the first time
					if ( data[voxelPointer] != index || !DrawFaceZP( yAccess, voxelPointer, maxZ, netXAccess ) )
					{
						adjacentRowIsIdentical = false;
						break;
					}

					voxelPointer++;
					yAccess++;
				}

				if ( !adjacentRowIsIdentical )
					break;

				// We found a whole row that's valid!
				length_b++;

				// Remember we've meshed these faces
				var tempZP = originalZP;
				tempZP += AccessStepX * g;

				for ( int h = 0; h < length_a; h++ )
				{
					meshVisiter.visitZP[tempZP] = comparison;
					tempZP += AccessStepY;
				}
			}

			WriteVertex( i, j, k + 1, 5, textureID );
			WriteVertex( i + length_b, j, k + 1, 5, textureID );
			WriteVertex( i, j + length_a, k + 1, 5, textureID );

			WriteVertex( i, j + length_a, k + 1, 5, textureID );
			WriteVertex( i + length_b, j, k + 1, 5, textureID );
			WriteVertex( i + length_b, j + length_a, k + 1, 5, textureID );
		}

		// Bottom (Y-)
		if ( meshVisiter.visitYN[visitYN] != comparison && DrawFaceYN( voxel, minY, xAccess, zAccess ) )
		{
			var originalYN = visitYN;

			// Remember we've meshed this face
			meshVisiter.visitYN[visitYN] = comparison;
			visitYN += AccessStepX;

			// Combine faces along the X axis
			var voxelPointer = access + AccessStepX;
			var netXAccess = xAccess + AccessStepX;

			for ( end_a = i1; end_a < Constants.ChunkSize; end_a++ )
			{
				if ( data[voxelPointer] != index || !DrawFaceYN( voxelPointer, minY, netXAccess, zAccess ) || meshVisiter.visitYN[visitYN] == comparison )
					break;

				// Remember we've meshed this face
				meshVisiter.visitYN[visitYN] = comparison;
				visitYN += AccessStepX;

				// Move 1 unit on the X axis
				voxelPointer += AccessStepX;
				netXAccess += AccessStepX;
			}

			// Calculate how many voxels we combined along the X axis
			var length_a = end_a - i1 + 1;

			// Combine faces along the Z axis
			length_b = 1;

			var max_length_b = Constants.ChunkSize - k;

			for ( int g = 1; g < max_length_b; g++ )
			{
				// Go back to where we started, then move g units on the Z axis
				voxelPointer = access;
				voxelPointer += AccessStepZ * g;
				netXAccess = xAccess;

				// Check if the entire row next to us is also the same index and not covered by another block
				bool adjacentRowIsIdentical = true;

				for ( var test_a = i; test_a < end_a; test_a++ )
				{
					// No need to check *yp here as we're combining on this axis for the first time
					if ( data[voxelPointer] != index || !DrawFaceYN( voxelPointer, minY, netXAccess, zAccess ) )
					{
						adjacentRowIsIdentical = false;
						break;
					}

					voxelPointer += AccessStepX;
					netXAccess += AccessStepX;
				}

				if ( !adjacentRowIsIdentical )
					break;

				// We found a whole row that's valid!
				length_b++;

				// Remember we've meshed these faces
				var tempYN = originalYN;
				tempYN += AccessStepZ * g;

				for ( int h = 0; h < length_a; h++ )
				{
					meshVisiter.visitYN[tempYN] = comparison;
					tempYN += AccessStepX;
				}
			}

			WriteVertex( i, j, k, 1, textureID );
			WriteVertex( i + length_a, j, k, 1, textureID );
			WriteVertex( i, j, k + length_b, 1, textureID );

			WriteVertex( i, j, k + length_b, 1, textureID );
			WriteVertex( i + length_a, j, k, 1, textureID );
			WriteVertex( i + length_a, j, k + length_b, 1, textureID );
		}

		// Top (Y+)
		if ( meshVisiter.visitYP[visitYP] != comparison && DrawFaceYP( voxel, maxY, xAccess, zAccess ) )
		{
			var originalYP = visitYP;

			// Remember we've meshed this face
			meshVisiter.visitYP[visitYP] = comparison;
			visitYP += AccessStepX;

			// Combine faces along the X axis
			var voxelPointer = access + AccessStepX;
			var netXAccess = xAccess + AccessStepX;

			for ( end_a = i1; end_a < Constants.ChunkSize; end_a++ )
			{
				if ( data[voxelPointer] != index || !DrawFaceYP( voxelPointer, maxY, netXAccess, zAccess ) || meshVisiter.visitYP[visitYP] == comparison )
					break;

				// Remember we've meshed this face
				meshVisiter.visitYP[visitYP] = comparison;
				visitYP += AccessStepX;

				// Move 1 unit on the X axis
				voxelPointer += AccessStepX;
				netXAccess += AccessStepX;
			}

			// Calculate how many voxels we combined along the X axis
			var length_a = end_a - i1 + 1;

			// Combine faces along the Z axis
			length_b = 1;

			var max_length_b = Constants.ChunkSize - k;

			for ( int g = 1; g < max_length_b; g++ )
			{
				// Go back to where we started, then move g units on the Z axis
				voxelPointer = access;
				voxelPointer += AccessStepZ * g;
				netXAccess = xAccess;


				// Check if the entire row next to us is also the same index and not covered by another block
				bool adjacentRowIsIdentical = true;

				for ( var test_a = i; test_a < end_a; test_a++ )
				{
					// No need to check *yp here as we're combining on this axis for the first time
					if ( data[voxelPointer] != index || !DrawFaceYP( voxelPointer, maxY, netXAccess, zAccess ) )
					{
						adjacentRowIsIdentical = false;
						break;
					}

					voxelPointer += AccessStepX;
					netXAccess += AccessStepX;
				}

				if ( !adjacentRowIsIdentical )
					break;

				// We found a whole row that's valid!
				length_b++;

				// Remember we've meshed these faces
				var tempYP = originalYP;
				tempYP += AccessStepZ * g;

				for ( int h = 0; h < length_a; h++ )
				{
					meshVisiter.visitYP[tempYP] = comparison;
					tempYP += AccessStepX;
				}
			}

			WriteVertex( i, j + 1, k, 0, textureID );
			WriteVertex( i, j + 1, k + length_b, 0, textureID );
			WriteVertex( i + length_a, j + 1, k, 0, textureID );

			WriteVertex( i + length_a, j + 1, k, 0, textureID );
			WriteVertex( i, j + 1, k + length_b, 0, textureID );
			WriteVertex( i + length_a, j + 1, k + length_b, 0, textureID );
		}
	}

	protected static bool DrawFaceCommon( byte index ) => index == 0;

	protected bool DrawFaceXN( int j, int bPointer, bool min, int kCS2 )
	{
		if ( min )
		{
			if ( !ChunkXN.Allocated )
				return true;

			return DrawFaceCommon( ChunkXN.Voxels[(Constants.ChunkSize - 1) * Constants.ChunkSize + j + kCS2] );
		}

		return DrawFaceCommon( Chunk.Voxels[bPointer - Constants.ChunkSize] );
	}

	protected bool DrawFaceXP( int j, int bPointer, bool max, int kCS2 )
	{
		if ( max )
		{
			if ( !ChunkXP.Allocated )
				return true;

			return DrawFaceCommon( ChunkXP.Voxels[j + kCS2] );
		}

		return DrawFaceCommon( Chunk.Voxels[bPointer + Constants.ChunkSize] );
	}

	protected bool DrawFaceYN( int bPointer, bool min, int iCS, int kCS2 )
	{
		if ( min )
		{
			if ( !ChunkYN.Allocated )
				return true;

			return DrawFaceCommon( ChunkYN.Voxels[iCS + (Constants.ChunkSize - 1) + kCS2] );
		}

		return DrawFaceCommon( Chunk.Voxels[bPointer - AccessStepY] );
	}

	protected bool DrawFaceYP( int voxelPointer, bool max, int xAccess, int zAccess )
	{
		if ( max )
		{
			if ( !ChunkYP.Allocated )
				return true;

			return DrawFaceCommon( ChunkYP.Voxels[xAccess + zAccess] );
		}

		return DrawFaceCommon( Chunk.Voxels[voxelPointer + AccessStepY] );
	}

	protected bool DrawFaceZN( int j, int bPointer, bool min, int iCS )
	{
		if ( min )
		{
			if ( !ChunkZN.Allocated )
				return true;

			return DrawFaceCommon( ChunkZN.Voxels[iCS + j + (Constants.ChunkSize - 1) * Constants.ChunkSizeSquared] );
		}

		return DrawFaceCommon( Chunk.Voxels[bPointer - Constants.ChunkSizeSquared] );
	}

	protected bool DrawFaceZP( int j, int bPointer, bool max, int iCS )
	{
		if ( max )
		{
			if ( !ChunkZP.Allocated )
				return true;

			return DrawFaceCommon( ChunkZP.Voxels[iCS + j] );
		}

		return DrawFaceCommon( Chunk.Voxels[bPointer + Constants.ChunkSizeSquared] );
	}
}
