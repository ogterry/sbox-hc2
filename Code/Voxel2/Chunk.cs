using System;
using Sandbox.Diagnostics;

namespace Voxel;

public partial class Chunk
{
	public byte[] Voxels;

	public bool Allocated => Voxels != null && MinAltitude != null && MaxAltitude != null;

	public bool Dirty;
	public bool Fake;

	public byte ChunkPosX;
	public byte ChunkPosY;
	public byte ChunkPosZ;

	public byte[] MaxAltitude;
	public byte[] MinAltitude;

	public void SetDirty() => Dirty = true;
	public void UnsetDirty() => Dirty = false;
	public bool IsDirty() => Dirty;

	public static int WorldToLocal( int a ) => a & Constants.ChunkMask;
	public static int GetHeightmapAccess( int i, int k ) => i | (k * Constants.ChunkSize);

	public Vector3Int WorldMin => new( ChunkPosX << Constants.ChunkShift, ChunkPosY << Constants.ChunkShift, ChunkPosZ << Constants.ChunkShift );
	public Vector3Int WorldMax => WorldMin + Constants.ChunkSize;

	public void Reset( int cx, int cy, int cz, bool fake )
	{
		Assert.True( !Allocated );

		ChunkPosX = (byte)cx;
		ChunkPosY = (byte)cy;
		ChunkPosZ = (byte)cz;

		Voxels = new byte[Constants.ChunkSizeCubed];
		SetDirty();

		Fake = fake;

		MinAltitude = new byte[Constants.ChunkSizeSquared];
		MaxAltitude = new byte[Constants.ChunkSizeSquared];

		Array.Fill( MinAltitude, (byte)Constants.ChunkSize );
	}

	public void SetVoxel( int x, int y, int z, byte index )
	{
		var i = WorldToLocal( x );
		var j = WorldToLocal( y );
		var k = WorldToLocal( z );

		Voxels[GetAccessLocal( i, j, k )] = index;

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

	public byte GetVoxel( int x, int y, int z )
	{
		var i = WorldToLocal( x );
		var j = WorldToLocal( y );
		var k = WorldToLocal( z );

		return Voxels[GetAccessLocal( i, j, k )];
	}

	public void Clear()
	{
		Array.Fill( Voxels, (byte)0 );
		Array.Fill( MinAltitude, (byte)Constants.ChunkSize );
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

		var min = MinAltitude[access];
		if ( j < min )
			MinAltitude[access] = (byte)j;

		var max = MaxAltitude[access];
		if ( j >= max )
			MaxAltitude[access] = (byte)j;
	}

	public void OnVoxelRemoved( int i, int j, int k )
	{
		var hAccess = GetHeightmapAccess( i, k );
		var xzAccess = i * Constants.ChunkSize + k * Constants.ChunkSizeSquared;
		var access = xzAccess + j;

		UpdateMinHeightmap( j, hAccess, access, xzAccess );
		UpdateMaxHeightmap( j, hAccess, access, xzAccess );
	}

	void UpdateMinHeightmap( int j, int hAccess, int access, int xzAccess )
	{
		var min = MinAltitude[hAccess];

		if ( min != j && min != (byte)Constants.ChunkSize )
			return;

		var chunkTop = xzAccess + Constants.ChunkSize;
		var amount = chunkTop - access;
		var ptr = access;

		for ( ; amount > 0; amount-- )
		{
			if ( Voxels[ptr++] == 0 )
				continue;

			MinAltitude[hAccess] = (byte)(access & Constants.ChunkMask);
			return;
		}

		MinAltitude[hAccess] = Constants.ChunkSize;
	}

	void UpdateMaxHeightmap( int j, int hAccess, int access, int xzAccess )
	{
		var max = MaxAltitude[hAccess];

		if ( max != j && max != 0 )
			return;

		var chunkBottom = xzAccess;
		var amount = access - chunkBottom;
		var ptr = access;

		for ( ; amount > 0; amount-- )
		{
			if ( Voxels[ptr--] == 0 )
				continue;

			MaxAltitude[hAccess] = (byte)(access & Constants.ChunkMask);
			return;
		}

		MaxAltitude[hAccess] = 0;
	}
}
