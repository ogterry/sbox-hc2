
using System;

namespace HC2.Mobs;

#nullable enable

[Icon( "accessibility_new" )]
public sealed class MobSpawner : Component
{
	[Property] public GameObject? Prefab { get; set; }

	[Property] public int MaxAlive { get; set; } = 1;

	[Property] public RangedFloat SpawnPeriod { get; set; } = 10f;

	[Property] public float Radius { get; set; } = 128f;

	private TimeUntil _nextSpawn;
	private readonly List<GameObject> _spawned = new();

	public int SpawnedCount => _spawned.Count;

	protected override void OnStart()
	{
		_nextSpawn = SpawnPeriod.GetValue();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy || !Prefab.IsValid() ) return;

		UpdateSpawnedList();

		if ( SpawnedCount >= MaxAlive )
		{
			_nextSpawn = SpawnPeriod.GetValue();
			return;
		}

		if ( _nextSpawn < 0f )
		{
			Spawn();
		}
	}

	private void UpdateSpawnedList()
	{
		for ( var i = _spawned.Count - 1; i >= 0; i-- )
		{
			if ( !_spawned[i].IsValid() )
			{
				_spawned.RemoveAt( i );
			}
		}
	}

	public void Spawn()
	{
		if ( Prefab is null ) return;

		_nextSpawn = SpawnPeriod.GetValue();

		var pos = MobHelpers.GetNearbyPosition( Transform.Position, 0f, Radius, 128f );
		var inst = Prefab.Clone( pos, Rotation.FromYaw( Random.Shared.Float( 0f, 360f ) ) );

		_spawned.Add( inst );
	}

	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected ) return;

		Gizmo.Transform = new Transform( Transform.Position, Rotation.FromPitch( 90f ) );
		Gizmo.Draw.Color = Color.Red.WithAlpha( 0.25f );
		Gizmo.Draw.SolidCircle( Vector3.Zero, Radius, sections: 12 );
	}
}

