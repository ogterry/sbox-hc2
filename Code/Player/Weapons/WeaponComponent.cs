using Sandbox.Citizen;

public abstract class WeaponComponent : Component
{
	/// <summary>
	/// What button do we use to attack with this weapon?
	/// </summary>
	[Property, InputAction] public string InputAction { get; set; } = "Attack1";

	/// <summary>
	/// What's the base attack delay for this weapon?
	/// </summary>
	[Property] public float AttackDelay { get; set; } = 1f;

	/// <summary>
	/// How do we hold this weapon?
	/// </summary>
	[Property] public CitizenAnimationHelper.HoldTypes HoldType { get; set; } = CitizenAnimationHelper.HoldTypes.None;

	[Property] public float Damage { get; set; } = 50f;

	[Property] public DamageType DamageType { get; set; }

	/// <summary>
	/// How long since we attacked?
	/// </summary>
	TimeSince TimeSinceAttack { get; set; }

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
		// TODO: Make this better
		if ( Player.IsValid() )
		{
			Player.HoldType = HoldType;
		}

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
		return Damage;
	}

	/// <summary>
	/// Attack!
	/// </summary>
	protected virtual void Attack()
	{
		TimeSinceAttack = 0;
		BroadcastEffects();
	}

	[Broadcast]
	protected void BroadcastEffects()
	{
		if ( Player.IsValid() )
		{
			if ( Player.ModelRenderer.IsValid() )
			{
				Player.ModelRenderer.Set( "b_attack", true );
			}
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
