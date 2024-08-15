using HC2;
using Sandbox.Citizen;
using Sandbox.Events;

public abstract class WeaponComponent : Carriable
{
	/// <summary>
	/// What button do we use to attack with this weapon?
	/// </summary>
	[Property, Group( "Input" ), InputAction] public string InputAction { get; set; } = "Attack1";

	/// <summary>
	/// What's the base attack delay for this weapon?
	/// </summary>
	[Property, Group( "Stats" )] public float AttackDelay { get; set; } = 1f;

	/// <summary>
	/// How much damage does this weapon inflict?
	/// </summary>
	[Property, Group( "Stats" )] public float Damage { get; set; } = 50f;

	/// <summary>
	/// What type of damage?
	/// </summary>
	[Property, Group( "Stats" )] public DamageType DamageType { get; set; }

	/// <summary>
	/// How much durability does this weapon lose on use?
	/// </summary>
	[Property, Group( "Stats" )] public float DurabilityOnUse { get; set; } = 0f;

	/// <summary>
	/// Is aiming enabled for this weapon?
	/// </summary>
	[Property, Group( "Aiming" )] public bool AimingEnabled { get; set; }

	/// <summary>
	/// Aiming FOV offset
	/// </summary>
	[Property, Group( "Aiming" ), ShowIf( nameof( AimingEnabled ), true ), Title( "Aiming FOV Offset" )] public float AimingFOVOffset { get; set; }

	/// <summary>
	/// The shoot sound
	/// </summary>
	[Property, Group( "Audio" )] public SoundEvent AttackSound { get; set; }

	/// <summary>
	/// How long since we attacked?
	/// </summary>
	protected TimeSince TimeSinceAttack { get; set; }

	/// <summary>
	/// Gets the player
	/// </summary>
	public Player Player => GameObject.Root.Components.Get<Player>();

	/// <summary>
	/// This is a method because the player's skills could increase attack speed.
	/// </summary>
	/// <returns></returns>
	protected virtual float GetAttackDelay()
	{
		return AttackDelay;
	}

	protected virtual DamageType GetDamageType()
	{
		return DamageType;
	}

	protected override void OnFixedUpdate()
	{
		// We don't care about doing anything if we're non-local
		if ( IsProxy )
			return;

		if ( Input.Down( InputAction ) )
		{
			if ( CanAttack() )
			{
				Attack();
			}
		}
	}

	protected DamageInstance ConstructDamage( SceneTraceResult tr )
	{
		return new DamageInstance()
		{
			Type = GetDamageType(),
			Damage = GetDamage(),
			Attacker = Player,
			Victim = tr.Component,
			Position = tr.HitPosition,
			Inflictor = this
		};
	}

	/// <summary>
	/// This is a method because the player could have buffs / stats that increase their damage.
	/// </summary>
	/// <returns></returns>
	protected float GetDamage()
	{
		float damage = Damage;
		bool willModify = true;
		var myModifier = Components.Get<StatModifier>();
		if ( myModifier.IsValid() )
		{
			damage += myModifier.DamageIncrease;
			damage *= myModifier.DamageMultiplier;
			if ( !myModifier.IsAffectedByStatusEffects ) willModify = false;
		}
		if ( willModify && GameObject.Root.Components.TryGet<StatModifier>( out var modifier ) )
		{
			damage += modifier.DamageIncrease;
			damage *= modifier.DamageMultiplier;
		}
		return damage;
	}

	/// <summary>
	/// Attack!
	/// </summary>
	protected virtual void Attack()
	{
		TimeSinceAttack = 0;
		GameObject.Root.Dispatch( new ItemUseEvent( DurabilityOnUse ) );
		BroadcastEffects();
	}

	[Broadcast]
	protected virtual void BroadcastEffects()
	{
		if ( Player.IsValid() )
		{
			if ( Player.ModelRenderer.IsValid() )
			{
				Player.ModelRenderer.Set( "b_attack", true );
			}
		}

		if ( AttackSound is not null )
		{
			Sound.Play( AttackSound, Transform.Position );
		}
	}

	/// <summary>
	/// Can we attack?
	/// </summary>
	/// <returns></returns>
	protected virtual bool CanAttack()
	{
		return TimeSinceAttack > GetAttackDelay();
	}
}
