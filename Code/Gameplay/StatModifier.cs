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
	/// How much max health is increased by.
	/// </summary>
	public float HealthIncrease { get; private set; } = 0f;
	/// <summary>
	/// How much health regen is increased by (per second).
	/// </summary>
	public float HealthRegenIncrease { get; private set; } = 0f;

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
				case StatusEffectType.ActionGraph:
					entry.OnEffectRemoved?.Invoke( GameObject );
					break;
			}
		}
	}
}
