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
	IGameEventHandler<ModifyDamageEvent>,
	IGameEventHandler<DamageTakenEvent>,
	IGameEventHandler<KilledEvent>
{
	public Transform SpawnTransform { get; private set; }

	public MobTarget? AimTarget { get; private set; }
	public Vector3? MoveTarget { get; private set; }

	public bool HasAimTarget => AimTarget.IsValid();
	public bool HasMoveTarget => MoveTarget is not null;

	[Property]
	public TextRenderer? FaceText { get; set; }

	[Property, Range( 0f, 4f )] public float DamageScale { get; set; } = 1f;

	[Property, KeyProperty]
	public event Action<DamageTakenEvent>? DamageTaken;

	[Property, KeyProperty]
	public event Action<KilledEvent>? Killed;

	private DamageTakenEvent? _lastDamageEvent;
	private TimeSince _sinceLastDamage;

	public bool HasTakenDamage => _lastDamageEvent is not null && _sinceLastDamage < 0.1f;
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

	[Broadcast( NetPermission.HostOnly )]
	public void SetFace( string value )
	{
		if ( FaceText is not null )
		{
			FaceText.Text = value;
		}
	}

	void IGameEventHandler<ModifyDamageEvent>.OnGameEvent( ModifyDamageEvent eventArgs )
	{
		eventArgs.DamageInstance.Damage *= DamageScale;
	}

	void IGameEventHandler<DamageTakenEvent>.OnGameEvent( DamageTakenEvent eventArgs )
	{
		DamageTaken?.Invoke( eventArgs );

		_lastDamageEvent = eventArgs;
		_sinceLastDamage = 0f;
	}

	void IGameEventHandler<KilledEvent>.OnGameEvent( KilledEvent eventArgs )
	{
		Killed?.Invoke( eventArgs );

		GameObject.Destroy();
	}
}
