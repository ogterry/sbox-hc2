using System;
using Sandbox.Utility;
using Sandbox.Diagnostics;
using System.Runtime.InteropServices;

namespace Voxel;

public class VoxelRenderer : Component, Component.ExecuteInEditor
{
	VoxelModel Model;

	protected override void OnEnabled()
	{
		base.OnEnabled();

		Transform.OnTransformChanged += OnLocalTransformChanged;

		Model?.Destroy();
		Model = new VoxelModel( 32, 32, 32 );

		for ( int x = 0; x < 32; x++ )
		{
			for ( int z = 0; z < 32; z++ )
			{
				float noiseValue = Noise.Fbm( 8, x * 0.4f, z * 0.4f );
				int surfaceHeight = (int)(noiseValue * 32);
				surfaceHeight = Math.Clamp( surfaceHeight, 0, 31 );

				for ( int y = 0; y < surfaceHeight; y++ )
				{
					float caveNoise = Noise.Perlin( x * 1.0f, y * 1.0f, z * 1.0f );

					if ( caveNoise < 0.4f )
						continue;

					byte blockType;

					if ( y < surfaceHeight - 4 )
					{
						blockType = 2;
					}
					else if ( y < surfaceHeight - 1 )
					{
						blockType = 1;
					}
					else
					{
						blockType = 3;
					}

					Model.AddVoxel( z, y, x, blockType );
				}
			}
		}

		var transform = Transform.World;

		foreach ( var mesh in Model.MeshChunks )
		{
			MeshChunk( mesh, transform );
		}
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();

		Transform.OnTransformChanged -= OnLocalTransformChanged;

		DestroyInternal();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		DestroyInternal();
	}

	private void DestroyInternal()
	{
		Model?.Destroy();
		Model = null;
	}

	private void OnLocalTransformChanged()
	{
		var transform = Transform.World;

		foreach ( var mesh in Model.MeshChunks )
		{
			if ( mesh == null )
				continue;

			var so = mesh.SceneObject;
			if ( so.IsValid() )
				so.Transform = transform.ToWorld( mesh.Transform );

			var body = mesh.PhysicsBody;
			if ( body.IsValid() )
				body.Transform = transform.ToWorld( mesh.Transform );
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
		c.PostMeshing( Scene.SceneWorld, Scene.PhysicsWorld, transform );
	}
}

public partial class VoxelModel
{
	public ChunkMesh[] MeshChunks = new ChunkMesh[Constants.MaxChunkAmountCubed];

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

	public static readonly Chunk EmptyChunk;
	public static readonly Chunk FullChunk;

	public bool ShouldMeshExterior = true;

	[StructLayout( LayoutKind.Sequential, Pack = 0 )]
	public struct PaletteMaterial
	{
		public Color Color;
		public int TextureIndex;
		public int Pad1;
		public int Pad2;
		public int Pad3;
	}

	public ComputeBuffer<PaletteMaterial> PaletteBuffer;

	static Texture texture;
	static Texture texture2;

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
		texture = Texture.Create( 1, 1 ).WithData( new byte[] { 255, 0, 0, 255 } ).Finish();
		texture2 = Texture.Create( 1, 1 ).WithData( new byte[] { 0, 255, 0, 255 } ).Finish();

		var palette = new PaletteMaterial[256];
		palette[0] = new PaletteMaterial { Color = new Color( 1.0f, 1.0f, 1.0f ), TextureIndex = texture.Index };
		palette[1] = new PaletteMaterial { Color = new Color( 0.9f, 0.5f, 0.0f ), TextureIndex = texture2.Index };
		palette[2] = new PaletteMaterial { Color = new Color( 1.0f, 1.0f, 1.0f ), TextureIndex = texture2.Index };

		PaletteBuffer = new ComputeBuffer<PaletteMaterial>( 256 );
		PaletteBuffer.SetData( palette );

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
		foreach ( var chunk in MeshChunks )
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
			Assert.True( MeshChunks[chunkAccess] != null );
			return chunk;
		}

		Assert.True( !chunk.Fake );

		chunk.Reset( f, g, h, false );
		Assert.True( chunk.Allocated );

		MeshChunks[chunkAccess] = new ChunkMesh( this, chunk );

		Assert.True( chunk.Voxels != null );

		return chunk;
	}
}
