
using System;

namespace HC2;


[GameResource( "Status Effect", "status", "Defines a Status Effect that can be applied to Players, Mobs, Weapons, Armour, ect." )]
public class StatusEffect : GameResource
{
	public enum StatusType
	{
		[Description( "An effect that is applied temporarily (via an item, armour, projectile, ect)" )]
		Buff,
		[Description( "A stat that you can upgrade when you level up. These are automatically shown in the Stat Upgrades menu and will stack." )]
		Stat
	}
	/// <summary>
	/// The name of the status effect as shown when hovering over it.
	/// </summary>
	public string Name { get; set; } = "New Status Effect";

	public StatusType Type { get; set; } = StatusType.Buff;

	/// <summary>
	/// A list of all effects that are applied when this status effect is applied to something.
	/// </summary>
	public List<StatusEffectEntry> Effects { get; set; } = new();

	public void Apply( GameObject gameObject )
	{
		var modifier = gameObject.Components.GetOrCreate<StatModifier>();
		if ( modifier is null )
			return;

		modifier.AddEffect( this );
	}

	public void Remove( GameObject gameObject )
	{
		var modifier = gameObject.Components.Get<StatModifier>();
		if ( modifier is null )
			return;

		modifier.RemoveEffect( this );
	}
}

public enum StatusEffectType
{
	ActionGraph,
	DamageIncrease,
	DamageMultiplier,
	DamageReduction,
	HealthRegenIncrease,
	MaxHealthIncrease,
}

public struct StatusEffectEntry
{
	public StatusEffectType Type { get; set; }

	[HideIf( "Type", StatusEffectType.ActionGraph )] public float Value { get; set; }
	[ShowIf( "Type", StatusEffectType.ActionGraph )] public Action<GameObject> OnEffectApplied { get; set; }
	[ShowIf( "Type", StatusEffectType.ActionGraph )] public Action<GameObject> OnEffectRemoved { get; set; }

	public override string ToString()
	{
		var name = $"{Type}";
		if ( Type == StatusEffectType.DamageReduction )
			name += $" -{Value}%";
		else if ( Type == StatusEffectType.HealthRegenIncrease )
			name += $" +{Value}/s";
		else if ( Type != StatusEffectType.ActionGraph )
			name += $" +{Value}";
		return name;
	}
}
