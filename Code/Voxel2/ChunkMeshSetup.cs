using System;
using Sandbox.Diagnostics;

namespace Voxel;

public partial class ChunkMesh
{
	SceneObject SceneObject;

	public void PreMeshing()
	{
		Assert.True( chunk.voxels != null );
		Assert.True( chunk.dirty );

		chunk.UnsetDirty();
	}

	public void PostMeshing( SceneWorld scene )
	{
		// Create a buffer if we meshed any faces
		var size = VertexCount;
		if ( size > 0 )
		{
			if ( !SceneObject.IsValid() )
			{
				var modelBuilder = new ModelBuilder();
				var material = Material.Load( "materials/voxel.vmat" );
				var mesh = new Mesh( material );

				var boundsMin = Vector3.Zero;
				var boundsMax = boundsMin + (Constants.ChunkSize * 16);
				mesh.Bounds = new BBox( boundsMin, boundsMax );

				mesh.CreateVertexBuffer( size, VoxelVertex.Layout, TempData.AsSpan() );
				modelBuilder.AddMesh( mesh );
				var model = modelBuilder.Create();

				SceneObject = new SceneObject( scene, model, new Transform( new Vector3( WorldPos.y, WorldPos.z, WorldPos.x ) * 16 ) );
				SceneObject.Flags.CastShadows = true;
				SceneObject.Flags.IsOpaque = true;
				SceneObject.Flags.IsTranslucent = false;
				SceneObject.Attributes.Set( "VoxelSize", 16 );
			}
		}

		MeshVisiterAllocator.Recycle( ref meshVisiter );
	}

	public void Destroy()
	{
		if ( SceneObject.IsValid() )
		{
			SceneObject.Delete();
			SceneObject = null;
		}
	}
}
