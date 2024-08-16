namespace HC2;

public sealed class StatModifier : Component
{
	/// <summary>
	/// How much damage is increased by.
	/// </summary>
	public float DamageIncrease { get; private set; } = 0f;
	/// <summary>
	/// How much damage is multiplied by.
	/// </summary>
	public float DamageMultiplier { get; private set; } = 1f;

	/// <summary>
	/// How much incoming damage is reduced by.
	/// </summary>
	public float DamageReduction { get; private set; } = 0f;

	/// <summary>
	/// Whether or not any status effects affect this.
	/// </summary>
	[Property] public bool IsAffectedByStatusEffects { get; set; } = true;

	List<StatusEffect> StatusEffects { get; set; } = new();

	public void AddEffect( StatusEffect effect )
	{
		StatusEffects.Add( effect );

		foreach ( var entry in effect.Effects )
		{
			switch ( entry.Type )
			{
				case StatusEffectType.DamageIncrease:
					DamageIncrease += entry.Value;
					break;
				case StatusEffectType.HealthRegenIncrease:
					{
						var healthComponent = GameObject.Components.Get<HealthComponent>();
						if ( healthComponent.IsValid() )
							healthComponent.RegenRate += entry.Value;
					}
					break;
				case StatusEffectType.MaxHealthIncrease:
					{
						var healthComponent = GameObject.Components.Get<HealthComponent>();
						if ( healthComponent.IsValid() )
							healthComponent.MaxHealth += entry.Value;
					}
					break;
				case StatusEffectType.ActionGraph:
					entry.OnEffectApplied?.Invoke( GameObject );
					break;
			}
		}
	}

	public void RemoveEffect( StatusEffect effect )
	{
		StatusEffects.Remove( effect );

		foreach ( var entry in effect.Effects )
		{
			switch ( entry.Type )
			{
				case StatusEffectType.DamageIncrease:
					DamageIncrease -= entry.Value;
					break;
				case StatusEffectType.HealthRegenIncrease:
					{
						var healthComponent = GameObject.Components.Get<HealthComponent>();
						if ( healthComponent.IsValid() )
							healthComponent.RegenRate -= entry.Value;
					}
					break;
				case StatusEffectType.MaxHealthIncrease:
					{
						var healthComponent = GameObject.Components.Get<HealthComponent>();
						if ( healthComponent.IsValid() )
							healthComponent.MaxHealth -= entry.Value;
						break;
					}
				case StatusEffectType.ActionGraph:
					entry.OnEffectRemoved?.Invoke( GameObject );
					break;
			}
		}
	}

	public int GetStatusCount( StatusEffect effect )
	{
		return StatusEffects.Count( x => x == effect );
	}
}
