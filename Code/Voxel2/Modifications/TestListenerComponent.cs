
using System.Reflection.Metadata;

namespace Voxel.Modifications;

/// <summary>
/// When used in a prefab spawned by <see cref="VoxelWorldGen.SpawnPrefab"/>,
/// optionally moves the attached object up / down to match ground level, and
/// flattens the ground around it.
/// </summary>
[Icon( "vertical_align_bottom" )]
public sealed class TestListenerComponent : Component
{
	/// <summary>
	/// Region to listen for.
	/// </summary>
	[Property]
	public BBox Area { get; set; } = new BBox( -Vector3.One * 64f, Vector3.One * 64f );
	
	private VoxelChangeListener Listener { get; set; }

	protected override void OnStart()
	{
		var renderer = Scene.GetAllComponents<VoxelRenderer>().FirstOrDefault();
		var min = Transform.Position + Area.Mins;
		var max = Transform.Position + Area.Maxs;

		var voxelMins = renderer.WorldToVoxelCoords( min );
		var voxelMaxs = renderer.WorldToVoxelCoords( max );
		
		Listener = renderer.AddChangeListener( voxelMins, voxelMaxs );
		Listener.OnChange = ( m ) => { };
		
		base.OnStart();
	}

	protected override void OnDestroy()
	{
		Listener?.Dispose();
		Listener = null;
		
		base.OnDestroy();
	}

	protected override void OnUpdate()
	{
		using ( Gizmo.Scope( "this", Transform.Position ) )
		{
			Gizmo.Draw.Color = Color.Yellow;
		
			var min = Area.Mins;
			var max = Area.Maxs;
		
			Gizmo.Draw.SolidBox( new BBox( min, max ) );
		}
		
		base.OnUpdate();
	}

	protected override void DrawGizmos()
	{
		Gizmo.Draw.Color = Color.Yellow;
		
		var min = Area.Mins;
		var max = Area.Maxs;
		
		Gizmo.Draw.SolidBox( new BBox( min, max ) );
		base.DrawGizmos();
	}
}

