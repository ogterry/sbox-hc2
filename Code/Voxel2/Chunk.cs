using Sandbox.Diagnostics;

namespace Voxel;

public partial class Chunk
{
	public byte[] voxels;

	public bool Allocated => voxels != null && minAltitude != null && maxAltitude != null;

	public bool dirty;
	public bool fake;

	public byte chunkPosX;
	public byte chunkPosY;
	public byte chunkPosZ;

	public byte[] maxAltitude;
	public byte[] minAltitude;

	public void SetDirty() => dirty = true;
	public void UnsetDirty() => dirty = false;
	public bool IsDirty() => dirty;

	public static int WorldToLocal( int a ) => a & Constants.ChunkMask;
	public static int GetHeightmapAccess( int i, int k ) => i | (k * Constants.ChunkSize);

	public void Reset( int cx, int cy, int cz, bool fake )
	{
		// Ensure we were disposed properly before being re-allocated
		Assert.True( !Allocated );

		// Store the world position of this chunk
		chunkPosX = (byte)cx;
		chunkPosY = (byte)cy;
		chunkPosZ = (byte)cz;

		// Allocate voxel data
		voxels = new byte[Constants.BYTES_PER_CHUNK];
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
		voxels[access] = index;

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

	public void OnVoxelAdded( int i, int j, int k )
	{
		int access = GetHeightmapAccess( i, k );

		var min = minAltitude[access];
		if ( j < min )
			minAltitude[access] = (byte)j;

		var max = maxAltitude[access];
		if ( j >= max )
			maxAltitude[access] = (byte)j;
	}

	public void OnVoxelRemoved( int i, int j, int k )
	{
		// Precalculate 1D array accesses
		var hAccess = GetHeightmapAccess( i, k );
		var xzAccess = i * Constants.ChunkSize + k * Constants.ChunkSizeSquared;
		var access = xzAccess + j;

		// Update the min
		UpdateMinHeightmap( j, hAccess, access, xzAccess );

		// Update the max
		UpdateMaxHeightmap( j, hAccess, access, xzAccess );
	}

	void UpdateMinHeightmap( int j, int hAccess, int access, int xzAccess )
	{
		var min = minAltitude[hAccess];

		// Bail if we didn't remove the lowest voxel
		if ( min != j && min != (byte)Constants.ChunkSize )
			return;

		// Calculate how far up to search
		var chunkTop = xzAccess + Constants.ChunkSize;
		var amount = chunkTop - access;
		var ptr = access;

		// Search up until we find a voxel
		for ( ; amount > 0; amount-- )
		{
			if ( voxels[ptr++] == 0 )
				continue;

			// Store the Y part of the access in the heightmap
			minAltitude[hAccess] = (byte)(access & Constants.ChunkMask);
			return;
		}

		// If no voxel was found
		minAltitude[hAccess] = (byte)Constants.ChunkSize;
	}

	void UpdateMaxHeightmap( int j, int hAccess, int access, int xzAccess )
	{
		var max = maxAltitude[hAccess];

		// Bail if we didn't remove the highest voxel
		if ( max != j && max != 0 )
			return;

		// Calculate how far down to search
		var chunkBottom = xzAccess;
		var amount = access - chunkBottom;
		var ptr = access;

		// Search down until we find a voxel
		for ( ; amount > 0; amount-- )
		{
			if ( voxels[ptr--] == 0 )
				continue;

			// Store the Y part of the access in the heightmap
			maxAltitude[hAccess] = (byte)(access & Constants.ChunkMask);
			return;
		}

		// If no voxel was found
		maxAltitude[hAccess] = 0;
	}
}
