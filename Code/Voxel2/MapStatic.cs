using System;

namespace Voxel;

public partial class Map
{
	public static Chunk EMPTY_CHUNK; // Helper chunk for raytracing through a null chunk
	public static Chunk FULL_CHUNK;  // Helper chunk to prevent meshing the outside of a map

	static Map()
	{
		// Allocate an empty and a full chunk
		EMPTY_CHUNK = new Chunk();
		EMPTY_CHUNK.Reset( 0, 0, 0, true );

		FULL_CHUNK = new Chunk();
		FULL_CHUNK.Reset( 0, 0, 0, true );

		var voxel = new Voxel { index = 1 };
		Array.Fill( FULL_CHUNK.voxels, voxel );
	}
}
