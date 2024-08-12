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
	private float GetAttackDelay()
	{
		return AttackDelay;
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
