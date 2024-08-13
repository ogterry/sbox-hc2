using System;
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

	protected override void OnStart()
	{
		UpdateDepletion( 1f );
	}

	private float GetWeaponEffectiveness( DamageInstance damageInfo )
	{
		Log.Info( damageInfo.Inflictor );

		if ( damageInfo.Inflictor is not { } inflictor ) return 0f;

		return inflictor.Components.GetAll<ResourceGatherer>()
			.Where( x => x.SourceKind == SourceKind )
			.Select( x => x.Effectiveness )
			.DefaultIfEmpty( 0f )
			.Max();
	}

	void IGameEventHandler<ModifyDamageEvent>.OnGameEvent( ModifyDamageEvent eventArgs )
	{
		if ( Health.Health <= 0f )
		{
			eventArgs.ClearDamage();
			return;
		}

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

		var health = Health.Health - eventArgs.Instance.Damage;

		UpdateDepletion( Math.Clamp( health / Health.MaxHealth, 0f, 1f ));
	}

	private void DropItem( Vector3 direction )
	{
		Log.Info( "Dropped!" );

		// TODO: drop in world

		var player = Scene.GetAllComponents<Player>()
			.MinBy( x => x.Transform.Position - Transform.Position );

		if ( player is null )
		{
			return;
		}

		player.Inventory.GiveItem( HC2.Item.Create( Item ) );
	}

	void IGameEventHandler<KilledEvent>.OnGameEvent( KilledEvent eventArgs )
	{
		_sinceDepleted = 0f;
	}

	[Broadcast( NetPermission.HostOnly )]
	private void UpdateDepletion( float fraction )
	{
		var found = false;
		var stages = Components.GetAll<ResourceDepletionStage>( FindMode.EverythingInChildren )
			.OrderBy( x => x.Fraction );

		foreach ( var stage in stages )
		{
			if ( !found && stage.Fraction >= fraction )
			{
				found = true;
				stage.GameObject.Enabled = true;
			}
			else
			{
				stage.GameObject.Enabled = false;
			}
		}
	}
}

public sealed class ResourceDepletionStage : Component
{
	[Property, Range( 0f, 1f )] public float Fraction { get; set; } = 1f;

	protected override void OnValidate()
	{
		GameObject.Name = $"{Fraction * 100f:F0}%";
	}
}
