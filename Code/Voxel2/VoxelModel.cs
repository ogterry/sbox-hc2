using System;
using Sandbox.Diagnostics;
using System.Runtime.InteropServices;

namespace Voxel;

public partial class VoxelRenderer : Component, Component.ExecuteInEditor
{
	internal VoxelModel Model { get; private set; }

	[Property] public Vector3Int Size { get; set; } = new( 256, 32, 256 );

	[Property, MakeDirty] public Palette Palette { get; set; }

	public bool IsReady => Enabled && Model is not null;

	protected override void OnDirty()
	{
		base.OnDirty();

		Model?.SetPalette( Palette );
	}

	protected override void OnEnabled()
	{
		Transform.OnTransformChanged += OnLocalTransformChanged;

		Palette.OnReload += OnDirty;

		Model?.Destroy();
		Model = new VoxelModel( Size.x, Size.y, Size.z );
		Model.SetPalette( Palette );
	}

	protected override void OnDisabled()
	{
		Transform.OnTransformChanged -= OnLocalTransformChanged;

		DestroyInternal();
	}

	protected override void OnDestroy()
	{
		DestroyInternal();
	}

	private void DestroyInternal()
	{
		Palette.OnReload -= OnDirty;

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

	public void MeshChunks()
	{
		// TODO: have a set of dirty ones?

		var transform = Transform.World;

		var meshChunks = Model.MeshChunks;

		for ( var i = 0; i < meshChunks.Length; ++i )
		{
			var mesh = meshChunks[i];

			if ( mesh is null ) continue;

			if ( !mesh.Chunk.Allocated )
			{
				mesh.Destroy();
				meshChunks[i] = null;
				continue;
			}

			MeshChunk( mesh, transform );
		}
	}

	public Vector3Int WorldToVoxelCoords( Vector3 worldPos )
	{
		var voxelSize = Constants.VoxelSize;

		var localPos = Transform.World.PointToLocal( worldPos );

		return new( (int)MathF.Floor( localPos.y / voxelSize ), (int)MathF.Floor( localPos.z / voxelSize ), (int)MathF.Floor( localPos.x / voxelSize ) );
	}

	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected ) return;

		Gizmo.Draw.LineBBox( new BBox( 0f, new Vector3( Size.x, Size.z, Size.y ) * 16f ) );

		if ( Model is null ) return;

		Gizmo.Draw.Color = Color.White.WithAlpha( 0.125f );

		foreach ( var chunk in Model.Chunks )
		{
			if ( chunk is not { Allocated: true } ) continue;

			Gizmo.Draw.LineBBox( new BBox(
				new Vector3( chunk.WorldMin.x, chunk.WorldMin.z, chunk.WorldMin.y ) * 16f,
				new Vector3( chunk.WorldMax.x, chunk.WorldMax.z, chunk.WorldMax.y ) * 16f ) );
		}
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
		public Vector2 TextureSize;
		public int TextureIndex;
		public int Pad3;
	}

	public ComputeBuffer<PaletteMaterial> PaletteBuffer;
	private readonly PaletteMaterial[] Palette = new PaletteMaterial[256];

	static readonly Texture WhiteTexture;

	public void SetPalette( Palette palette )
	{
		if ( palette == null )
		{
			WhiteTexture.MarkUsed( int.MaxValue );
			Array.Fill( Palette, new PaletteMaterial { Color = Color.White, TextureIndex = WhiteTexture.Index, TextureSize = Vector2.One } );

			PaletteBuffer.SetData( Palette );

			return;
		}

		for ( var i = 0; i < palette.Materials.Count; ++i )
		{
			var material = palette.Materials[i];
			material.Texture?.MarkUsed( int.MaxValue );

			Palette[i] = new PaletteMaterial
			{
				Color = material.Color,
				TextureIndex = material.Texture != null ? material.Texture.Index : WhiteTexture.Index,
				TextureSize = material.TextureSize,
			};
		}

		PaletteBuffer.SetData( Palette );
	}

	static VoxelModel()
	{
		EmptyChunk = new Chunk();
		EmptyChunk.Reset( 0, 0, 0, true );

		FullChunk = new Chunk();
		FullChunk.Reset( 0, 0, 0, true );

		Array.Fill( FullChunk.Voxels, (byte)1 );

		WhiteTexture = Texture.Create( 1, 1 ).
			WithData( new byte[] { 255, 255, 255, 255 } ).
			Finish();

		WhiteTexture.MarkUsed( int.MaxValue );
	}

	public VoxelModel( int mx, int my, int mz )
	{
		WhiteTexture.MarkUsed( int.MaxValue );
		Array.Fill( Palette, new PaletteMaterial { Color = Color.White, TextureIndex = WhiteTexture.Index } );

		PaletteBuffer = new ComputeBuffer<PaletteMaterial>( 256 );
		PaletteBuffer.SetData( Palette );

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
	public bool OutOfBounds( Vector3Int voxelCoords ) => (uint)voxelCoords.x >= SizeX || (uint)voxelCoords.y >= SizeY || (uint)voxelCoords.z >= SizeZ;
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

	public Vector3Int GetChunkCoords( Vector3Int voxelCoords )
	{
		return new Vector3Int( voxelCoords.x >> Constants.ChunkShift, voxelCoords.y >> Constants.ChunkShift, voxelCoords.z >> Constants.ChunkShift );
	}

	public Vector3Int ClampVoxelCoords( Vector3Int voxelCoords )
	{
		return new Vector3Int(
			Math.Clamp( voxelCoords.x, 0, SizeX - 1 ),
			Math.Clamp( voxelCoords.y, 0, SizeY - 1 ),
			Math.Clamp( voxelCoords.z, 0, SizeZ - 1 ) );
	}

	public Vector3Int ClampChunkCoords( Vector3Int chunkCoords )
	{
		return new Vector3Int(
			Math.Clamp( chunkCoords.x, 0, ChunkAmountX - 1 ),
			Math.Clamp( chunkCoords.y, 0, ChunkAmountY - 1 ),
			Math.Clamp( chunkCoords.z, 0, ChunkAmountZ - 1 ) );
	}

	public void AddVoxel( int x, int y, int z, byte index )
	{
		if ( OutOfBounds( x, y, z ) )
			return;

		var chunk = InitChunk( x, y, z );
		chunk.SetVoxel( x, y, z, index );
	}

	public byte GetVoxel( int x, int y, int z )
	{
		if ( OutOfBounds( x, y, z ) )
			return 0;

		var chunk = Chunks[GetAccess( x, y, z )];
		return chunk.GetVoxel( x, y, z );
	}

	public void SetRegionDirty( Vector3Int min, Vector3Int max )
	{
		// Extend by 1 each direction to allow neighbouring chunks to update
		// I'm probably off by 1 here somewhere ;)

		var chunkMin = ClampChunkCoords( GetChunkCoords( min - 1 ) );
		var chunkMax = ClampChunkCoords( GetChunkCoords( max + 1 ) );

		for ( var f = chunkMin.x; f <= chunkMax.x; f++ )
		for ( var g = chunkMin.y; g <= chunkMax.y; g++ )
		for ( var h = chunkMin.z; h <= chunkMax.z; h++ )
		{
			GetChunkLocal( f, g, h ).SetDirty();
		}
	}

	public Chunk InitChunk( int x, int y, int z )
	{
		var f = x >> Constants.ChunkShift;
		var g = y >> Constants.ChunkShift;
		var h = z >> Constants.ChunkShift;

		return InitChunkLocal( f, g, h );
	}

	public Chunk GetChunkLocal( int f, int g, int h )
	{
		var chunkAccess = GetAccessLocal( f, g, h );
		var chunk = Chunks[chunkAccess];

		return chunk;
	}

	public Chunk InitChunkLocal( int f, int g, int h )
	{
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
