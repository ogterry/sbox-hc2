using System;
using Sandbox.Events;

namespace HC2.Mobs;

#nullable enable

[Icon( "adjust" )]
public sealed class MobTarget : Component
{

}

[Icon( "mood_bad" )]
public sealed class Mob : Component,
	IGameEventHandler<DamageTakenEvent>,
	IGameEventHandler<KilledEvent>
{
	public Transform SpawnTransform { get; private set; }

	public MobTarget? AimTarget { get; private set; }
	public Vector3? MoveTarget { get; private set; }

	public bool HasAimTarget => AimTarget.IsValid();
	public bool HasMoveTarget => MoveTarget is not null;

	[Property, KeyProperty]
	public event Action<DamageTakenEvent>? DamageTaken;

	[Property, KeyProperty]
	public event Action<KilledEvent>? Killed;

	private DamageTakenEvent? _lastDamageEvent;

	public bool HasTakenDamage => _lastDamageEvent is not null;
	public MobTarget? LastAttacker => _lastDamageEvent?.Instance.Attacker?.Components.GetInAncestorsOrSelf<MobTarget>();

	protected override void OnStart()
	{
		SpawnTransform = Transform.World;
	}

	public void SetMoveTarget( Vector3 position )
	{
		MoveTarget = position;
	}

	public void ClearMoveTarget()
	{
		MoveTarget = null;
	}

	public void SetAimTarget( MobTarget target )
	{
		AimTarget = target;
	}

	public void ClearAimTarget()
	{
		AimTarget = null;
	}

	protected override void OnFixedUpdate()
	{
		_lastDamageEvent = null;
	}

	void IGameEventHandler<DamageTakenEvent>.OnGameEvent( DamageTakenEvent eventArgs )
	{
		DamageTaken?.Invoke( eventArgs );

		_lastDamageEvent = eventArgs;
	}

	void IGameEventHandler<KilledEvent>.OnGameEvent( KilledEvent eventArgs )
	{
		Killed?.Invoke( eventArgs );

		GameObject.Destroy();
	}
}
