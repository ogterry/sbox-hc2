using Sandbox.Events;
using System;

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

	[Property] public int ExperienceYield { get; set; } = 6;

	[Property, KeyProperty]
	public event Action<DamageTakenEvent>? DamageTaken;

	[Property, KeyProperty]
	public event Action<KilledEvent>? Killed;

	[Property]
	SoundEvent? IdleSound { get; set; }

	[Property]
	SoundEvent? HitSound { get; set; }

	[Property]
	public List<MobDrop>? ItemDrops { get; set; } = new();

	private DamageTakenEvent? _lastDamageEvent;
	private TimeSince _sinceLastDamage;
	private TimeUntil _nextIdleSound;

	public bool HasTakenDamage => _lastDamageEvent is not null && _sinceLastDamage < 0.1f;
	public MobTarget? LastAttacker => _lastDamageEvent?.Instance.Attacker?.Components.GetInAncestorsOrSelf<MobTarget>();

	protected override void OnStart()
	{
		SpawnTransform = Transform.World;
		_nextIdleSound = Random.Shared.Float( 5f, 15f );
	}

	protected override void OnFixedUpdate()
	{
		if ( IdleSound != null && _nextIdleSound <= 0f )
		{
			Sound.Play( IdleSound, Transform.Position );
			_nextIdleSound = Random.Shared.Float( 5f, 15f );
		}
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

	void GiveNearbyPlayersExperience()
	{
		if ( !Sandbox.Networking.IsHost ) return;

		// TODO: Determine if we want larger enemies (bosses) to have a larger yield range
		var players = Scene.GetAllComponents<PlayerExperience>().Where( x => x.Transform.Position.Distance( Transform.Position ) < 2500f );
		foreach ( var player in players )
		{
			player.GivePoints( ExperienceYield );
		}
	}

	[Broadcast( NetPermission.HostOnly )]
	public void SetFace( string value )
	{
		if ( FaceText is not null )
		{
			FaceText.Text = value;
		}
	}

	[Broadcast]
	public void BroadcastEnableNametag()
	{
		if ( Components.TryGet<MobNametag>( out var nametag, FindMode.EverythingInChildren ) )
		{
			nametag.GameObject.Enabled = true;
		}
		_isNametagVisible = true;
	}
	bool _isNametagVisible = false;

	void IGameEventHandler<ModifyDamageEvent>.OnGameEvent( ModifyDamageEvent eventArgs )
	{
		eventArgs.DamageInstance.Damage *= DamageScale;
	}

	void IGameEventHandler<DamageTakenEvent>.OnGameEvent( DamageTakenEvent eventArgs )
	{
		DamageTaken?.Invoke( eventArgs );

		_lastDamageEvent = eventArgs;
		_sinceLastDamage = 0f;

		if ( !_isNametagVisible && eventArgs.Instance.Damage > 0f )
			BroadcastEnableNametag();

		if ( HitSound != null )
		{
			Sound.Play( HitSound, Transform.Position );
		}
	}

	bool isDead = false;
	void IGameEventHandler<KilledEvent>.OnGameEvent( KilledEvent eventArgs )
	{
		// Needed to add this because the event was getting called 3 times -Carson
		if ( isDead ) return;
		isDead = true;

		GiveNearbyPlayersExperience();
		Killed?.Invoke( eventArgs );

		if ( ItemDrops is { Count: > 0 } )
		{
			foreach ( var drop in ItemDrops )
			{
				if ( Random.Shared.Float( 0f, 100f ) <= drop.Chance )
				{
					var amount = Random.Shared.Int( drop.AmountRange.x, drop.AmountRange.y );
					var item = WorldItem.CreateInstance( drop.Item, Transform.Position + Vector3.Up * 16f );
				}
			}
		}

		BroadcastCreateGibs( eventArgs.LastDamage.Force );
	}

	[Broadcast( NetPermission.HostOnly )]
	void BroadcastCreateGibs( Vector3 force )
	{
		foreach ( var go in GameObject.GetAllObjects( true ) )
		{
			if ( !go.Tags.Has( "bodypart" ) )
				continue;

			if ( go.Parent?.Tags?.Has( "bodypart" ) ?? false )
				continue;

			var gib = go.Clone( new CloneConfig()
			{
				Transform = go.Transform.World.WithScale( go.Transform.Parent.Transform.Scale ),
				StartEnabled = true
			} );
			var rb = gib.Components.GetOrCreate<Rigidbody>();
			rb.Velocity = (force * Random.Shared.Float( 0.8f, 1.2f ) * 150f) / rb.PhysicsBody.Mass;
			rb.AngularVelocity = Vector3.Random * Random.Shared.Float( 0.8f, 1.2f ) * 10f;
			var mobGib = gib.Components.Create<MobGib>();
			mobGib.Sound = HitSound;
		}
		GameObject.Destroy();
	}
}

public class MobDrop
{
	[KeyProperty] public ItemAsset? Item { get; set; }
	[KeyProperty] public Vector2Int AmountRange { get; set; } = 1;
	public float Chance { get; set; } = 100f;
}
