using Sandbox.Diagnostics;

namespace Voxel;

public partial class Chunk
{
	public void Reset( int cx, int cy, int cz, bool fake )
	{
		// Ensure we were disposed properly before being re-allocated
		Assert.True( !Allocated );

		// Store the world position of this chunk
		chunkPosX = (byte)cx;
		chunkPosY = (byte)cy;
		chunkPosZ = (byte)cz;

		// Allocate voxel data
		bytes_voxels = Constants.BYTES_PER_CHUNK;
		voxels = new Voxel[bytes_voxels];
		SetDirty();

		// Two fake chunks are allocated, one that's completely empty and one that's full
		// The empty chunk is used for raytracing, and full chunk is used to prevent meshing the outside of the map
		this.fake = fake;

		// To speed up meshing, each chunk stores two heightmaps, which contain the min and max altitude of each column in the chunk.
		//  This saves looping over the whole chunk when meshing
		minAltitude = new byte[Constants.ChunkSizeSquared];
		maxAltitude = new byte[Constants.ChunkSizeSquared];

		// Prepare the min heightmap
		var ptr = 0;
		for ( int i = Constants.ChunkSizeSquared; i > 0; i-- )
			minAltitude[ptr++] = Constants.ChunkSize;
	}

	public void SetVoxel( int x, int y, int z, byte index )
	{
		var i = WorldToLocal( x );
		var j = WorldToLocal( y );
		var k = WorldToLocal( z );

		// Update the voxel
		var access = GetAccessLocal( i, j, k );
		voxels[access] = new Voxel { index = index };

		if ( index > 0 )
		{
			OnVoxelAdded( i, j, k );
		}
		else
		{
			OnVoxelRemoved( i, j, k );
		}

		SetDirty();
	}

	// Shortcut functions
	public void SetDirty() => dirty = true;
	public void UnsetDirty() => dirty = false;
	public bool IsDirty() => dirty;

	public static int WorldToLocal( int a ) => a & Constants.ChunkMask;
	public static int GetHeightmapAccess( int i, int k ) => i | (k * Constants.ChunkSize);

	public static int GetAccessLocal( int i, int j, int k )
	{
		Assert.True( i >= 0 );
		Assert.True( j >= 0 );
		Assert.True( k >= 0 );

		Assert.True( i < Constants.ChunkSize );
		Assert.True( j < Constants.ChunkSize );
		Assert.True( k < Constants.ChunkSize );

		return j | (i * Constants.ChunkSize) | (k * Constants.ChunkSizeSquared);
	}
}
