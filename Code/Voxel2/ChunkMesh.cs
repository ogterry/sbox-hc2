using Sandbox.Diagnostics;
using System;

namespace Voxel;

public partial class ChunkMesh
{
	public VoxelModel Model;

	const int CHUNK_STEP_Y = 1;
	const int CHUNK_STEP_X = Constants.MaxChunkAmount;
	const int CHUNK_STEP_Z = Constants.MaxChunkAmountSquared;

	const int ACCESS_STEP_Y = 1;
	const int ACCESS_STEP_X = Constants.ChunkSize;
	const int ACCESS_STEP_Z = Constants.ChunkSizeSquared;

	public Chunk Chunk;

	Chunk ChunkXN;
	Chunk ChunkXP;
	Chunk ChunkYN;
	Chunk ChunkYP;
	Chunk ChunkZN;
	Chunk ChunkZP;

	int ChunkPosX => Chunk.chunkPosX;
	int ChunkPosY => Chunk.chunkPosY;
	int ChunkPosZ => Chunk.chunkPosZ;

	public Vector3 WorldPos;

	static readonly uint[] Buffer = new uint[Constants.ChunkSizeCubed * 6 * 6];
	int BufferWrite = 0;

	static readonly MeshVisiter meshVisiter = new();

	private SceneObject SceneObject;

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

		ChunkXN = ChunkPosX > 0 ? Model.chunks[chunkAccess - CHUNK_STEP_X] : exteriorChunk;
		ChunkXP = ChunkPosX < Model.ChunkAmountXM1 ? Model.chunks[chunkAccess + CHUNK_STEP_X] : exteriorChunk;

		ChunkYN = ChunkPosY > 0 ? Model.chunks[chunkAccess - CHUNK_STEP_Y] : exteriorChunk;
		ChunkYP = ChunkPosY < Model.ChunkAmountYM1 ? Model.chunks[chunkAccess + CHUNK_STEP_Y] : exteriorChunk;

		ChunkZN = ChunkPosZ > 0 ? Model.chunks[chunkAccess - CHUNK_STEP_Z] : exteriorChunk;
		ChunkZP = ChunkPosZ < Model.ChunkAmountZM1 ? Model.chunks[chunkAccess + CHUNK_STEP_Z] : exteriorChunk;
	}

	public void PreMeshing()
	{
		Assert.True( Chunk.voxels != null );
		Assert.True( Chunk.dirty );

		Chunk.UnsetDirty();
	}

	public void PostMeshing( SceneWorld scene )
	{
		// Create a buffer if we meshed any faces
		var size = BufferWrite;
		if ( size > 0 )
		{
			if ( !SceneObject.IsValid() )
			{
				var modelBuilder = new ModelBuilder();
				var material = Material.Load( "materials/voxel.vmat" );
				var mesh = new Mesh( material );

				var boundsMin = Vector3.Zero;
				var boundsMax = boundsMin + (Constants.ChunkSize * 16);
				mesh.Bounds = new BBox( boundsMin, boundsMax );

				mesh.CreateVertexBuffer( size, Layout, Buffer.AsSpan() );
				modelBuilder.AddMesh( mesh );
				var model = modelBuilder.Create();

				SceneObject = new SceneObject( scene, model, new Transform( new Vector3( WorldPos.z, WorldPos.x, WorldPos.y ) * Constants.VoxelSize ) );
				SceneObject.Flags.CastShadows = true;
				SceneObject.Flags.IsOpaque = true;
				SceneObject.Flags.IsTranslucent = false;
				SceneObject.Attributes.Set( "VoxelSize", Constants.VoxelSize );
			}
		}
	}

	public void Destroy()
	{
		if ( SceneObject.IsValid() )
		{
			SceneObject.Delete();
			SceneObject = null;
		}
	}

	void WriteVertex( int i, int j, int k, byte normal, byte kind )
	{
		byte brightness = 7;
		byte texture = (byte)(kind - 1);
		uint shared = (uint)((texture & 255) << 24 | (normal & 7) << 21 | (brightness & 7) << 18);
		Buffer[BufferWrite++] = shared | ((uint)j & 63) << 12 | ((uint)i & 63) << 6 | ((uint)k & 63);
	}

	public void GenerateMesh()
	{
		// Ensure this chunk exists
		Assert.True( Chunk.Allocated );
		Assert.True( !Chunk.fake );

		BufferWrite = 0;
		meshVisiter.Reset();

		// Precalculate Z voxel access
		var zAccess = 0;
		var maxYPointer = 0;
		var minYPointer = 0;

		for ( int k = 0; k < Constants.ChunkSize; k++ )
		{
			// Precalculate X voxel access
			var xAccess = 0;

			for ( int i = 0; i < Constants.ChunkSize; i++ )
			{
				// Get the min and max bounds for this column
				int j = Chunk.minAltitude[minYPointer++];
				int maxJ = Chunk.maxAltitude[maxYPointer++];

				// Precalculate voxel access
				var access = zAccess + xAccess + j;
				var voxel = access;

				// Mesh from the bottom to the top of this column
				for ( ; j <= maxJ; j++, access++, voxel++ )
				{
					var v = Chunk.voxels[voxel];
					if ( v > 0 )
						CreateRuns( voxel, i, j, k, access, xAccess, zAccess );
				}

				// Update voxel access
				xAccess += Constants.ChunkSize;
			}

			// Update voxel access
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
		var data = Chunk.voxels;
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
			visitXN += ACCESS_STEP_Y;


			// Combine faces upwards along the Y axis
			var voxelPointer = access + ACCESS_STEP_Y;
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
				visitXN += ACCESS_STEP_Y;
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
				voxelPointer += ACCESS_STEP_Z * g;
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
				tempXN += ACCESS_STEP_Z * g;

				for ( int h = 0; h < length_a; h++ )
				{
					meshVisiter.visitXN[tempXN] = comparison;
					tempXN += ACCESS_STEP_Y;
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
			visitXP += ACCESS_STEP_Y;

			// Combine faces along the Y axis
			var voxelPointer = access + ACCESS_STEP_Y;
			var yAccess = j1;

			for ( end_a = j1; end_a < Constants.ChunkSize; end_a++ )
			{
				if ( data[voxelPointer] != index || !DrawFaceXP( yAccess, voxelPointer, maxX, zAccess ) || meshVisiter.visitXP[visitXP] == comparison )
					break;

				voxelPointer++;
				yAccess++;

				// Remember we've meshed this face
				meshVisiter.visitXP[visitXP] = comparison;
				visitXP += ACCESS_STEP_Y;
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
				voxelPointer += ACCESS_STEP_Z * g;
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
				tempXP += ACCESS_STEP_Z * g;

				for ( int h = 0; h < length_a; h++ )
				{
					meshVisiter.visitXP[tempXP] = comparison;
					tempXP += ACCESS_STEP_Y;
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
			visitZN += ACCESS_STEP_Y;

			// Combine faces along the Y axis
			var voxelPointer = access + ACCESS_STEP_Y;
			var yAccess = j1;

			for ( end_a = j1; end_a < Constants.ChunkSize; end_a++ )
			{
				if ( data[voxelPointer] != index || !DrawFaceZN( yAccess, voxelPointer, minZ, xAccess ) || meshVisiter.visitZN[visitZN] == comparison )
					break;

				voxelPointer++;
				yAccess++;

				// Remember we've meshed this face
				meshVisiter.visitZN[visitZN] = comparison;
				visitZN += ACCESS_STEP_Y;
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
				voxelPointer += ACCESS_STEP_X * g;
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
				tempZN += ACCESS_STEP_X * g;

				for ( int h = 0; h < length_a; h++ )
				{
					meshVisiter.visitZN[tempZN] = comparison;
					tempZN += ACCESS_STEP_Y;
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
			visitZP += ACCESS_STEP_Y;

			// Combine faces along the Y axis
			var voxelPointer = access + ACCESS_STEP_Y;
			var yAccess = j1;

			for ( end_a = j1; end_a < Constants.ChunkSize; end_a++ )
			{
				if ( data[voxelPointer] != index || !DrawFaceZP( yAccess, voxelPointer, maxZ, xAccess ) || meshVisiter.visitZP[visitZP] == comparison )
					break;

				voxelPointer++;
				yAccess++;

				// Remember we've meshed this face
				meshVisiter.visitZP[visitZP] = comparison;
				visitZP += ACCESS_STEP_Y;
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
				voxelPointer += ACCESS_STEP_X * g;
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
				tempZP += ACCESS_STEP_X * g;

				for ( int h = 0; h < length_a; h++ )
				{
					meshVisiter.visitZP[tempZP] = comparison;
					tempZP += ACCESS_STEP_Y;
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
			visitYN += ACCESS_STEP_X;

			// Combine faces along the X axis
			var voxelPointer = access + ACCESS_STEP_X;
			var netXAccess = xAccess + ACCESS_STEP_X;

			for ( end_a = i1; end_a < Constants.ChunkSize; end_a++ )
			{
				if ( data[voxelPointer] != index || !DrawFaceYN( voxelPointer, minY, netXAccess, zAccess ) || meshVisiter.visitYN[visitYN] == comparison )
					break;

				// Remember we've meshed this face
				meshVisiter.visitYN[visitYN] = comparison;
				visitYN += ACCESS_STEP_X;

				// Move 1 unit on the X axis
				voxelPointer += ACCESS_STEP_X;
				netXAccess += ACCESS_STEP_X;
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
				voxelPointer += ACCESS_STEP_Z * g;
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

					voxelPointer += ACCESS_STEP_X;
					netXAccess += ACCESS_STEP_X;
				}

				if ( !adjacentRowIsIdentical )
					break;

				// We found a whole row that's valid!
				length_b++;

				// Remember we've meshed these faces
				var tempYN = originalYN;
				tempYN += ACCESS_STEP_Z * g;

				for ( int h = 0; h < length_a; h++ )
				{
					meshVisiter.visitYN[tempYN] = comparison;
					tempYN += ACCESS_STEP_X;
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
			visitYP += ACCESS_STEP_X;

			// Combine faces along the X axis
			var voxelPointer = access + ACCESS_STEP_X;
			var netXAccess = xAccess + ACCESS_STEP_X;

			for ( end_a = i1; end_a < Constants.ChunkSize; end_a++ )
			{
				if ( data[voxelPointer] != index || !DrawFaceYP( voxelPointer, maxY, netXAccess, zAccess ) || meshVisiter.visitYP[visitYP] == comparison )
					break;

				// Remember we've meshed this face
				meshVisiter.visitYP[visitYP] = comparison;
				visitYP += ACCESS_STEP_X;

				// Move 1 unit on the X axis
				voxelPointer += ACCESS_STEP_X;
				netXAccess += ACCESS_STEP_X;
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
				voxelPointer += ACCESS_STEP_Z * g;
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

					voxelPointer += ACCESS_STEP_X;
					netXAccess += ACCESS_STEP_X;
				}

				if ( !adjacentRowIsIdentical )
					break;

				// We found a whole row that's valid!
				length_b++;

				// Remember we've meshed these faces
				var tempYP = originalYP;
				tempYP += ACCESS_STEP_Z * g;

				for ( int h = 0; h < length_a; h++ )
				{
					meshVisiter.visitYP[tempYP] = comparison;
					tempYP += ACCESS_STEP_X;
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

			return DrawFaceCommon( ChunkXN.voxels[(Constants.ChunkSize - 1) * Constants.ChunkSize + j + kCS2] );
		}

		return DrawFaceCommon( Chunk.voxels[bPointer - Constants.ChunkSize] );
	}

	protected bool DrawFaceXP( int j, int bPointer, bool max, int kCS2 )
	{
		if ( max )
		{
			if ( !ChunkXP.Allocated )
				return true;

			return DrawFaceCommon( ChunkXP.voxels[j + kCS2] );
		}

		return DrawFaceCommon( Chunk.voxels[bPointer + Constants.ChunkSize] );
	}

	protected bool DrawFaceYN( int bPointer, bool min, int iCS, int kCS2 )
	{
		if ( min )
		{
			if ( !ChunkYN.Allocated )
				return true;

			return DrawFaceCommon( ChunkYN.voxels[iCS + (Constants.ChunkSize - 1) + kCS2] );
		}

		return DrawFaceCommon( Chunk.voxels[bPointer - ACCESS_STEP_Y] );
	}

	protected bool DrawFaceYP( int voxelPointer, bool max, int xAccess, int zAccess )
	{
		if ( max )
		{
			if ( !ChunkYP.Allocated )
				return true;

			return DrawFaceCommon( ChunkYP.voxels[xAccess + zAccess] );
		}

		return DrawFaceCommon( Chunk.voxels[voxelPointer + ACCESS_STEP_Y] );
	}

	protected bool DrawFaceZN( int j, int bPointer, bool min, int iCS )
	{
		if ( min )
		{
			if ( !ChunkZN.Allocated )
				return true;

			return DrawFaceCommon( ChunkZN.voxels[iCS + j + (Constants.ChunkSize - 1) * Constants.ChunkSizeSquared] );
		}

		return DrawFaceCommon( Chunk.voxels[bPointer - Constants.ChunkSizeSquared] );
	}

	protected bool DrawFaceZP( int j, int bPointer, bool max, int iCS )
	{
		if ( max )
		{
			if ( !ChunkZP.Allocated )
				return true;

			return DrawFaceCommon( ChunkZP.voxels[iCS + j] );
		}

		return DrawFaceCommon( Chunk.voxels[bPointer + Constants.ChunkSizeSquared] );
	}
}
