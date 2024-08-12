using System;
using Sandbox.Utility;
using Sandbox.Diagnostics;

namespace Voxel;

public class VoxelRenderer : Component, Component.ExecuteInEditor
{
	VoxelModel Model;

	protected override void OnEnabled()
	{
		base.OnEnabled();

		Transform.OnTransformChanged += OnLocalTransformChanged;

		Model = new VoxelModel( 128, 64, 128 );

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
					Model.AddVoxel( z, y, x, blockType );
				}
			}
		}

		var transform = Transform.World;

		foreach ( var mesh in Model.meshChunks )
		{
			MeshChunk( mesh, transform );
		}
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();

		Transform.OnTransformChanged -= OnLocalTransformChanged;

		Model.Destroy();
		Model = null;
	}

	private void OnLocalTransformChanged()
	{
		var transform = Transform.World;

		foreach ( var mesh in Model.meshChunks )
		{
			if ( mesh == null )
				continue;

			var so = mesh.SceneObject;
			if ( !so.IsValid() )
				continue;

			so.Transform = transform.ToWorld( mesh.Transform );
		}
	}

	protected void MeshChunk( ChunkMesh c, Transform transform )
	{
		if ( c == null )
			return;

		if ( !c.Chunk.IsDirty() )
			return;

		Assert.True( !c.Chunk.Fake );

		c.PreMeshing();
		c.GenerateMesh();
		c.PostMeshing( Scene.SceneWorld, transform );
	}
}

public partial class VoxelModel
{
	public ChunkMesh[] meshChunks = new ChunkMesh[Constants.MaxChunkAmountCubed];

	public int SizeX;
	public int SizeY;
	public int SizeZ;

	public int ChunkAmountX;
	public int ChunkAmountY;
	public int ChunkAmountZ;

	public int ChunkAmountXM1;
	public int ChunkAmountYM1;
	public int ChunkAmountZM1;

	public Chunk[] Chunks;

	public bool ShouldMeshExterior = true;

	public static readonly Chunk EmptyChunk;
	public static readonly Chunk FullChunk;

	static VoxelModel()
	{
		EmptyChunk = new Chunk();
		EmptyChunk.Reset( 0, 0, 0, true );

		FullChunk = new Chunk();
		FullChunk.Reset( 0, 0, 0, true );

		Array.Fill( FullChunk.Voxels, (byte)1 );
	}

	public VoxelModel( int mx, int my, int mz )
	{
		Chunks = new Chunk[Constants.MaxChunkAmountCubed];
		for ( int i = 0; i < Chunks.Length; i++ )
		{
			Chunks[i] = new Chunk();
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

		var chunk = InitChunk( x, y, z );
		chunk.SetVoxel( x, y, z, index );
	}

	public Chunk InitChunk( int x, int y, int z )
	{
		var f = x >> Constants.ChunkShift;
		var g = y >> Constants.ChunkShift;
		var h = z >> Constants.ChunkShift;

		var chunkAccess = GetAccessLocal( f, g, h );
		var chunk = Chunks[chunkAccess];

		if ( chunk.Allocated )
		{
			Assert.True( meshChunks[chunkAccess] != null );
			return chunk;
		}

		Assert.True( !chunk.Fake );

		chunk.Reset( f, g, h, false );
		Assert.True( chunk.Allocated );

		meshChunks[chunkAccess] = new ChunkMesh( this, chunk );

		Assert.True( chunk.Voxels != null );

		return chunk;
	}
}
