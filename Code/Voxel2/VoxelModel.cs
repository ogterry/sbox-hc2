using System;
using Sandbox.Diagnostics;
using Sandbox.Utility;

namespace Voxel;

public class VoxelRenderer : Component, Component.ExecuteInEditor
{
	VoxelModel m;

	protected override void OnEnabled()
	{
		base.OnEnabled();

		m = new VoxelModel( 128, 64, 128 );

		for ( int x = 0; x < 128; x++ )
		{
			for ( int z = 0; z < 128; z++ )
			{
				float noiseValue = Noise.Perlin( x * 1.0f, z * 1.0f );
				int height = (int)(noiseValue * 64);

				byte blockType;
				if ( noiseValue < 0.3f )
				{
					blockType = 1;
				}
				else if ( noiseValue < 0.6f )
				{
					blockType = 2;
				}
				else
				{
					blockType = 3;
				}

				for ( int y = 0; y < height; y++ )
				{
					m.AddVoxel( z, y, x, blockType );
				}
			}
		}


		foreach ( var mesh in m.meshChunks )
		{
			MeshChunk( mesh );
		}
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();

		m.Destroy();
		m = null;
	}

	protected void MeshChunk( ChunkMesh c )
	{
		if ( c == null )
			return;

		if ( !c.Chunk.IsDirty() )
			return;

		Assert.True( !c.Chunk.fake );

		c.PreMeshing();
		c.GenerateMesh();
		c.PostMeshing( Scene.SceneWorld );
	}
}

public partial class VoxelModel
{
	public ChunkMesh[] meshChunks = new ChunkMesh[Constants.MaxChunkAmountCubed];

	public long meshingTime;
	public int meshSize;
	public int meshedChunkCount;

	public int SizeX;
	public int SizeY;
	public int SizeZ;
	public int ChunkAmountX;
	public int ChunkAmountY;
	public int ChunkAmountZ;

	public int ChunkAmountXM1;
	public int ChunkAmountYM1;
	public int ChunkAmountZM1;

	public Chunk[] chunks;

	public bool ShouldMeshExterior = true;

	public static readonly Chunk EmptyChunk;
	public static readonly Chunk FullChunk;

	static VoxelModel()
	{
		// Allocate an empty and a full chunk
		EmptyChunk = new Chunk();
		EmptyChunk.Reset( 0, 0, 0, true );

		FullChunk = new Chunk();
		FullChunk.Reset( 0, 0, 0, true );

		Array.Fill( FullChunk.voxels, (byte)1 );
	}

	public VoxelModel( int mx, int my, int mz )
	{
		// Always allocate 32x32x32 chunks, even though we don't use them all
		chunks = new Chunk[Constants.MaxChunkAmountCubed];
		for ( int i = 0; i < chunks.Length; i++ )
		{
			chunks[i] = new Chunk();
		}

		Assert.True( mx % Constants.ChunkSize == 0 );
		Assert.True( my % Constants.ChunkSize == 0 );
		Assert.True( mz % Constants.ChunkSize == 0 );

		SizeX = mx;
		SizeY = my;
		SizeZ = mz;

		ChunkAmountX = SizeX / Constants.ChunkSize;
		ChunkAmountY = SizeY / Constants.ChunkSize;
		ChunkAmountZ = SizeZ / Constants.ChunkSize;
		ChunkAmountXM1 = ChunkAmountX - 1;
		ChunkAmountYM1 = ChunkAmountY - 1;
		ChunkAmountZM1 = ChunkAmountZ - 1;
	}

	public void Destroy()
	{
		foreach ( var chunk in meshChunks )
		{
			if ( chunk == null )
				continue;

			chunk.Destroy();
		}
	}

	public bool OutOfBounds( int x, int y, int z ) => (uint)x >= SizeX || (uint)y >= SizeY || (uint)z >= SizeZ;

	public int GetAccess( int x, int y, int z ) => GetAccessLocal( x >> Constants.ChunkShift, y >> Constants.ChunkShift, z >> Constants.ChunkShift );
	public int GetAccessLocal( int f, int g, int h )
	{
		if ( (uint)f >= ChunkAmountX || (uint)g >= ChunkAmountY || (uint)h >= ChunkAmountZ )
		{
			return -1;
		}

		var y0 = g;
		var x0 = f * Constants.MaxChunkAmount;
		var z0 = h * Constants.MaxChunkAmountSquared;

		return y0 | x0 | z0;
	}

	public void AddVoxel( int x, int y, int z, byte index )
	{
		if ( OutOfBounds( x, y, z ) )
			return;

		// Get and initialise (if null) the chunk
		var c = InitChunk( x, y, z );

		// Add a voxel to the chunk
		c.SetVoxel( x, y, z, index );
	}

	public Chunk InitChunk( int x, int y, int z )
	{
		// i, j, k are voxel positions within a chunk
		// f, g, h are chunk positions
		// x, y, z are global voxel positions
		var f = x >> Constants.ChunkShift;
		var g = y >> Constants.ChunkShift;
		var h = z >> Constants.ChunkShift;

		// Precalculate
		var chunkAccess = GetAccessLocal( f, g, h );
		var c = chunks[chunkAccess];

		// If already initialised, return it
		if ( c.Allocated )
		{
			Assert.True( meshChunks[chunkAccess] != null );
			return c;
		}

		// Ensure we're not trying to initialise the empty or full chunk
		Assert.True( !c.fake );

		// Initialise it and return it
		c.Reset( f, g, h, false );
		Assert.True( c.Allocated );

		// Create a meshing wrapper for this chunk
		meshChunks[chunkAccess] = new ChunkMesh( this, c );

		Assert.True( c.voxels != null );

		return c;
	}
}
