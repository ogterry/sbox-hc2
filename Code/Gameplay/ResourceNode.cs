using Sandbox.Events;

namespace HC2;

public enum GatherSourceKind
{
	Wood,
	Stone,
	Metal
}

public sealed class ResourceNode : Component,
	IGameEventHandler<ModifyDamageEvent>,
	IGameEventHandler<DamageTakenEvent>,
	IGameEventHandler<KilledEvent>
{
	[RequireComponent]
	public HealthComponent Health { get; private set; }

	[Property]
	public GatherSourceKind SourceKind { get; set; }

	[Property]
	public ItemAsset Item { get; set; }

	[Property]
	public int DamagePerItem { get; set; } = 10;

	private float _spareDamage;
	private TimeSince _sinceDepleted;

	private float GetWeaponEffectiveness( DamageInstance damageInfo )
	{
		if ( damageInfo.Inflictor is not { } inflictor ) return 0f;

		return inflictor.Components.GetAll<ResourceGatherer>()
			.Where( x => x.SourceKind == SourceKind )
			.Select( x => x.Effectiveness )
			.DefaultIfEmpty( 0f )
			.Max();
	}

	void IGameEventHandler<ModifyDamageEvent>.OnGameEvent( ModifyDamageEvent eventArgs )
	{
		var effectiveness = GetWeaponEffectiveness( eventArgs.DamageInstance );

		eventArgs.ScaleDamage( effectiveness );
	}

	void IGameEventHandler<DamageTakenEvent>.OnGameEvent( DamageTakenEvent eventArgs )
	{
		if ( DamagePerItem <= 0 || Health.Health <= 0f )
		{
			return;
		}

		_spareDamage += eventArgs.Instance.Damage;

		while ( _spareDamage >= DamagePerItem )
		{
			_spareDamage -= DamagePerItem;
			DropItem( (eventArgs.Instance.Position - Transform.Position).Normal );
		}
	}

	private void DropItem( Vector3 direction )
	{
		Log.Info( "Dropped!" );
	}

	void IGameEventHandler<KilledEvent>.OnGameEvent( KilledEvent eventArgs )
	{
		_sinceDepleted = 0f;
	}
}
