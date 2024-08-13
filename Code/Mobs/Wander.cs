
using System;

namespace HC2.Mobs;

#nullable enable

/// <summary>
/// Aimlessly mosey around.
/// </summary>
[Icon( "hiking" )]
public sealed class Wander : Component
{
	[RequireComponent] public Mob Mob { get; private set; } = null!;

	[Property] public float MaxRangeFromSpawn { get; set; } = 1024f;

	[Property] public float MinDistance { get; set; } = 256f;
	[Property] public float MaxDistance { get; set; } = 512f;

	[Property] public float MinPauseTime { get; set; } = 1f;
	[Property] public float MaxPauseTime { get; set; } = 2f;

	private TimeUntil _untilContinue;
	private bool _hadTarget;

	public bool IsPaused => !Mob.HasMoveTarget && _untilContinue > 0f;

	protected override void OnAwake()
	{
		Pause();
	}

	protected override void OnFixedUpdate()
	{
		if ( Mob.HasMoveTarget )
		{
			_hadTarget = true;
			return;
		}

		if ( _hadTarget )
		{
			Pause();
		}

		if ( IsPaused ) return;

		Resume();
	}

	private void Pause()
	{
		Mob.ClearMoveTarget();

		_untilContinue = Random.Shared.Float( MinPauseTime, MaxPauseTime );
		_hadTarget = false;
	}

	private void Resume()
	{
		var origin = Transform.Position;

		if ( (origin - Mob.SpawnTransform.Position).Length > MaxRangeFromSpawn )
		{
			origin = Mob.SpawnTransform.Position + (origin - Mob.SpawnTransform.Position).Normal * MaxRangeFromSpawn;
		}

		var target = MobHelpers.GetNearbyPosition( origin, MinDistance, MaxDistance, 64f );

		Mob.SetMoveTarget( target );
	}
}
