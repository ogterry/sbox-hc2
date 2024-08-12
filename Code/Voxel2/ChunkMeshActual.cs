using Sandbox.Diagnostics;

namespace Voxel;

public partial class ChunkMesh
{
	const int ACCESS_STEP_Y = 1;
	const int ACCESS_STEP_X = Constants.ChunkSize;
	const int ACCESS_STEP_Z = Constants.ChunkSizeSquared;

	static readonly uint[] TempData = new uint[Constants.ChunkSizeCubed * 6 * 6];
	int VertexCount = 0;

	void WriteVertex( int i, int j, int k, byte normal, byte kind )
	{
		byte brightness = 7;
		byte texture = (byte)(kind - 1);
		uint shared = (uint)((texture & 255) << 24 | (normal & 7) << 21 | (brightness & 7) << 18);

		// Adjust the bit shifts for Z-up:
		TempData[VertexCount++] = shared | ((uint)i & 63) << 12 | ((uint)k & 63) << 6 | ((uint)j & 63);
	}


	public void GenerateMesh()
	{
		// Ensure this chunk exists
		Assert.True( chunk.Allocated );
		Assert.True( !chunk.fake );

		VertexCount = 0;

		meshVisiter = MeshVisiterAllocator.Get();

		// Precalculate Z voxel access
		var zAccess = 0;

		// Get heightmap pointers that we can modify
		var maxYPointer = 0;
		var minYPointer = 0;

		for ( int k = 0; k < Constants.ChunkSize; k++ )
		{
			// Precalculate X voxel access
			var xAccess = 0;

			for ( int i = 0; i < Constants.ChunkSize; i++ )
			{
				// Get the min and max bounds for this column
				int j = chunk.minAltitude[minYPointer++];
				int maxJ = chunk.maxAltitude[maxYPointer++];

				// Precalculate voxel access
				var access = zAccess + xAccess + j;
				var voxel = access;

				// Mesh from the bottom to the top of this column
				for ( ; j <= maxJ; j++, access++, voxel++ )
				{
					var v = chunk.voxels[voxel];
					if ( v.index > 0 )
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
		var data = chunk.voxels;
		var index = data[voxel].index;
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
		if ( meshVisiter.visitXN[visitXN] != comparison && DrawFaceXN( j, voxel, minX, zAccess, index ) )
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
				if ( data[voxelPointer].index != index ||                              // It's a different kind of voxel
					!DrawFaceXN( yAccess, voxelPointer, minX, zAccess, index ) ||  // This voxel face is covered by another voxel
					meshVisiter.visitXN[visitXN] == comparison )                                      // We've already meshed this voxel face
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
					if ( data[voxelPointer].index != index || !DrawFaceXN( yAccess, voxelPointer, minX, netZAccess, index ) )
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

			WriteVertex( i, j, k, 1, textureID );
			WriteVertex( i, j, k + length_b, 1, textureID );
			WriteVertex( i, j + length_a, k, 1, textureID );

			WriteVertex( i, j + length_a, k, 1, textureID );
			WriteVertex( i, j, k + length_b, 1, textureID );
			WriteVertex( i, j + length_a, k + length_b, 1, textureID );
		}

		// Right (X+)
		if ( meshVisiter.visitXP[visitXP] != comparison && DrawFaceXP( j, voxel, maxX, zAccess, index ) )
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
				if ( data[voxelPointer].index != index || !DrawFaceXP( yAccess, voxelPointer, maxX, zAccess, index ) || meshVisiter.visitXP[visitXP] == comparison )
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
					if ( data[voxelPointer].index != index || !DrawFaceXP( yAccess, voxelPointer, maxX, netZAccess, index ) )
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

			WriteVertex( i + 1, j, k, 0, textureID );
			WriteVertex( i + 1, j + length_a, k, 0, textureID );
			WriteVertex( i + 1, j, k + length_b, 0, textureID );

			WriteVertex( i + 1, j, k + length_b, 0, textureID );
			WriteVertex( i + 1, j + length_a, k, 0, textureID );
			WriteVertex( i + 1, j + length_a, k + length_b, 0, textureID );
		}

		// Back (Z-)
		if ( meshVisiter.visitZN[visitZN] != comparison && DrawFaceZN( j, voxel, minZ, xAccess, index ) )
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
				if ( data[voxelPointer].index != index || !DrawFaceZN( yAccess, voxelPointer, minZ, xAccess, index ) || meshVisiter.visitZN[visitZN] == comparison )
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
					if ( data[voxelPointer].index != index || !DrawFaceZN( yAccess, voxelPointer, minZ, netXAccess, index ) )
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

			WriteVertex( i, j, k, 2, textureID );
			WriteVertex( i, j + length_a, k, 2, textureID );
			WriteVertex( i + length_b, j, k, 2, textureID );

			WriteVertex( i + length_b, j, k, 2, textureID );
			WriteVertex( i, j + length_a, k, 2, textureID );
			WriteVertex( i + length_b, j + length_a, k, 2, textureID );
		}

		// Front (Z+)
		if ( meshVisiter.visitZP[visitZP] != comparison && DrawFaceZP( j, voxel, maxZ, xAccess, index ) )
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
				if ( data[voxelPointer].index != index || !DrawFaceZP( yAccess, voxelPointer, maxZ, xAccess, index ) || meshVisiter.visitZP[visitZP] == comparison )
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
					if ( data[voxelPointer].index != index || !DrawFaceZP( yAccess, voxelPointer, maxZ, netXAccess, index ) )
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

			WriteVertex( i, j, k + 1, 3, textureID );
			WriteVertex( i + length_b, j, k + 1, 3, textureID );
			WriteVertex( i, j + length_a, k + 1, 3, textureID );

			WriteVertex( i, j + length_a, k + 1, 3, textureID );
			WriteVertex( i + length_b, j, k + 1, 3, textureID );
			WriteVertex( i + length_b, j + length_a, k + 1, 3, textureID );
		}

		// Bottom (Y-)
		if ( meshVisiter.visitYN[visitYN] != comparison && DrawFaceYN( voxel, minY, xAccess, zAccess, index ) )
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
				if ( data[voxelPointer].index != index || !DrawFaceYN( voxelPointer, minY, netXAccess, zAccess, index ) || meshVisiter.visitYN[visitYN] == comparison )
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
					if ( data[voxelPointer].index != index || !DrawFaceYN( voxelPointer, minY, netXAccess, zAccess, index ) )
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

			WriteVertex( i, j, k, 4, textureID );
			WriteVertex( i + length_a, j, k, 4, textureID );
			WriteVertex( i, j, k + length_b, 4, textureID );

			WriteVertex( i, j, k + length_b, 4, textureID );
			WriteVertex( i + length_a, j, k, 4, textureID );
			WriteVertex( i + length_a, j, k + length_b, 4, textureID );
		}

		// Top (Y+)
		if ( meshVisiter.visitYP[visitYP] != comparison && DrawFaceYP( voxel, maxY, xAccess, zAccess, index ) )
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
				if ( data[voxelPointer].index != index || !DrawFaceYP( voxelPointer, maxY, netXAccess, zAccess, index ) || meshVisiter.visitYP[visitYP] == comparison )
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
					if ( data[voxelPointer].index != index || !DrawFaceYP( voxelPointer, maxY, netXAccess, zAccess, index ) )
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

			WriteVertex( i, j + 1, k, 5, textureID );
			WriteVertex( i, j + 1, k + length_b, 5, textureID );
			WriteVertex( i + length_a, j + 1, k, 5, textureID );

			WriteVertex( i + length_a, j + 1, k, 5, textureID );
			WriteVertex( i, j + 1, k + length_b, 5, textureID );
			WriteVertex( i + length_a, j + 1, k + length_b, 5, textureID );
		}
	}

	protected bool DrawFaceCommon( Voxel nextPtr, byte index )
	{
		if ( nextPtr.index == 0 )
			return true;

		return false;
	}

	protected bool DrawFaceXN( int j, int bPointer, bool min, int kCS2, byte index )
	{
		// If it is outside this chunk, get the voxel from the neighbouring chunk
		if ( min )
		{
			if ( m.ShouldMeshBetweenChunks )
				return true;

			if ( !chunkXN.Allocated )
				return true;

			return DrawFaceCommon( chunkXN.voxels[(Constants.ChunkSize - 1) * Constants.ChunkSize + j + kCS2], index );
		}

		return DrawFaceCommon( chunk.voxels[bPointer - Constants.ChunkSize], index );
	}

	protected bool DrawFaceXP( int j, int bPointer, bool max, int kCS2, byte index )
	{
		if ( max )
		{
			if ( m.ShouldMeshBetweenChunks )
				return true;

			// If no chunk next to us, render
			if ( !chunkXP.Allocated )
				return true;

			return DrawFaceCommon( chunkXP.voxels[j + kCS2], index );
		}

		return DrawFaceCommon( chunk.voxels[bPointer + Constants.ChunkSize], index );
	}

	protected bool DrawFaceYN( int bPointer, bool min, int iCS, int kCS2, byte index )
	{
		if ( min )
		{
			if ( m.ShouldMeshBetweenChunks )
				return true;

			// If there's no chunk below us, render the face
			if ( !chunkYN.Allocated )
				return true;

			return DrawFaceCommon( chunkYN.voxels[iCS + (Constants.ChunkSize - 1) + kCS2], index );
		}

		return DrawFaceCommon( chunk.voxels[bPointer - ACCESS_STEP_Y], index );
	}

	protected bool DrawFaceYP( int voxelPointer, bool max, int xAccess, int zAccess, byte index )
	{
		if ( max )
		{
			if ( m.ShouldMeshBetweenChunks )
				return true;

			// If there's no chunk above us, render
			if ( !chunkYP.Allocated )
				return true;

			// Check if there's a block in the bottom layer of the chunk above us
			return DrawFaceCommon( chunkYP.voxels[xAccess + zAccess], index );
		}

		// Check if the block above us is the same index
		return DrawFaceCommon( chunk.voxels[voxelPointer + ACCESS_STEP_Y], index );
	}

	protected bool DrawFaceZN( int j, int bPointer, bool min, int iCS, byte index )
	{
		if ( min )
		{
			if ( m.ShouldMeshBetweenChunks )
				return true;

			// if there's no chunk next to us, render
			if ( !chunkZN.Allocated )
				return true;

			return DrawFaceCommon( chunkZN.voxels[iCS + j + (Constants.ChunkSize - 1) * Constants.ChunkSizeSquared], index );
		}

		return DrawFaceCommon( chunk.voxels[bPointer - Constants.ChunkSizeSquared], index );
	}

	protected bool DrawFaceZP( int j, int bPointer, bool max, int iCS, byte index )
	{
		if ( max )
		{
			if ( m.ShouldMeshBetweenChunks )
				return true;

			// If no chunk next to us, render
			if ( !chunkZP.Allocated )
				return true;

			return DrawFaceCommon( chunkZP.voxels[iCS + j], index );
		}

		return DrawFaceCommon( chunk.voxels[bPointer + Constants.ChunkSizeSquared], index );
	}
}
