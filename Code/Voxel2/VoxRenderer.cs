using Sandbox.Diagnostics;

namespace Voxel;

public partial class VoxRenderer : Component, Component.ExecuteInEditor
{
	internal VoxelModel Model { get; private set; }

	private VoxResource _resource;

	[Property]
	public VoxResource VoxModel
	{
		get => _resource;
		set
		{
			if ( _resource == value )
				return;

			_resource = value;

			if ( !GameObject.Flags.HasFlag( GameObjectFlags.Deserializing ) )
			{
				CreateModel();
			}
		}
	}

	protected override void OnDirty()
	{
		base.OnDirty();

		CreateModel();
	}

	private void CreateModel()
	{
		Model?.Destroy();

		if ( VoxModel == null )
			return;

		var mx = (VoxModel.Size.y + Constants.ChunkSize) / Constants.ChunkSize * Constants.ChunkSize;
		var my = (VoxModel.Size.z + Constants.ChunkSize) / Constants.ChunkSize * Constants.ChunkSize;
		var mz = (VoxModel.Size.x + Constants.ChunkSize) / Constants.ChunkSize * Constants.ChunkSize;

		Model = new VoxelModel( mx, my, mz );

		foreach ( var block in VoxModel.Blocks )
		{
			Model.AddVoxel( block.Y, block.Z, block.X, block.Index );
		}

		MeshChunks();

		Model.SetPalette( VoxModel.Palette );
	}

	private void OnModelReload( VoxResource model )
	{	
		if ( VoxModel != model )
			return;

		CreateModel();
	}

	protected override void OnEnabled()
	{
		Transform.OnTransformChanged += OnLocalTransformChanged;

		Palette.OnReload += OnDirty;
		VoxResource.OnReload += OnModelReload;

		if ( Model == null )
		{
			CreateModel();
		}
		else
		{
			foreach ( var mesh in Model.MeshChunks )
			{
				if ( mesh == null )
					continue;

				var so = mesh.SceneObject;
				if ( so.IsValid() )
					so.RenderingEnabled = true;
			}
		}
	}

	protected override void OnDisabled()
	{
		Transform.OnTransformChanged -= OnLocalTransformChanged;

		Palette.OnReload -= OnDirty;
		VoxResource.OnReload -= OnModelReload;

		foreach ( var mesh in Model.MeshChunks )
		{
			if ( mesh == null )
				continue;

			var so = mesh.SceneObject;
			if ( so.IsValid() )
				so.RenderingEnabled = false;
		}
	}

	protected override void OnDestroy()
	{
		Palette.OnReload -= OnDirty;
		VoxResource.OnReload -= OnModelReload;

		Model?.Destroy();
		Model = null;
	}

	private void OnLocalTransformChanged()
	{
		if ( Model == null )
			return;

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
		var transform = Transform.World;
		var meshChunks = Model.MeshChunks;

		for ( var i = 0; i < meshChunks.Length; ++i )
		{
			var mesh = meshChunks[i];

			if ( mesh is null )
				continue;

			if ( !mesh.Chunk.Allocated )
			{
				mesh.Destroy();
				meshChunks[i] = null;
				continue;
			}

			MeshChunk( mesh, transform );

			if ( mesh.PhysicsBody.IsValid() )
				mesh.PhysicsBody.SetComponentSource( this );
		}
	}

	protected override void DrawGizmos()
	{
		var tr = Scene.Trace.Ray( Gizmo.CurrentRay, Gizmo.RayDepth )
			.Run();

		if ( tr.Hit && tr.Component == this )
		{
			Gizmo.Hitbox.TrySetHovered( tr.HitPosition );
		}

		if ( !Gizmo.IsSelected )
			return;

		if ( VoxModel is not null )
		{
			Gizmo.Draw.LineBBox( new BBox( 0.0f, new Vector3( VoxModel.Size.x, VoxModel.Size.y, VoxModel.Size.z ) * Constants.VoxelSize ) );
		}

		if ( Model is null )
			return;

		Gizmo.Draw.Color = Color.White.WithAlpha( 0.125f );

		foreach ( var chunk in Model.Chunks )
		{
			if ( !chunk.Allocated )
				continue;

			Gizmo.Draw.LineBBox( new BBox(
				new Vector3( chunk.WorldMin.z, chunk.WorldMin.x, chunk.WorldMin.y ) * Constants.VoxelSize,
				new Vector3( chunk.WorldMax.z, chunk.WorldMax.x, chunk.WorldMax.y ) * Constants.VoxelSize ) );
		}
	}
}
