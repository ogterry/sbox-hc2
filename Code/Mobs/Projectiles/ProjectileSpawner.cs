
using System;

namespace HC2.Mobs;

#nullable enable

public sealed class ProjectileSpawner : Component
{
	[Property] public GameObject? Prefab { get; set; }

	[Property] public RangedFloat SpawnPeriod { get; set; } = 5f;

	[Property] public Vector3 SpawnOffset { get; set; }

	private TimeUntil _nextSpawn;
	private readonly List<GameObject> _spawned = new();

	protected override void OnStart()
	{
		_nextSpawn = SpawnPeriod.GetValue();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy || !Prefab.IsValid() ) return;

		if ( _nextSpawn < 0f )
		{
			Spawn();
		}
	}

	public void Spawn()
	{
		if ( Prefab is null ) return;

		_nextSpawn = SpawnPeriod.GetValue();

		var pos = Transform.Position + SpawnOffset;
		var inst = Prefab.Clone( pos );

		inst.NetworkSpawn();
	}

	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected ) return;

		Gizmo.Transform = new Transform( Transform.Position + SpawnOffset, Rotation.FromPitch( 90f ) );
		Gizmo.Draw.Color = Color.Red.WithAlpha( 0.5f );
		Gizmo.Draw.SolidSphere( Vector3.Zero, 5f );
	}
}

