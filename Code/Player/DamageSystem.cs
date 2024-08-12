using System;

[Flags]
public enum DamageFlags
{
	None = 0,

	/// <summary>
	/// This is a critical attack.
	/// </summary>
	Critical = 1,
}

public enum DamageType
{
	Blunt,
	Sharp,
	Explosive,
	Fire,
	Freeze,
	Poison,
	Electric,
	Radiation,
	Acid,
	Heal,
}

public static class DamageTypeExtensions
{
	public static Color GetColor( this DamageType type )
	{
		return type switch
		{
			DamageType.Blunt => Color.White,
			DamageType.Sharp => Color.Red,
			DamageType.Explosive => Color.Yellow,
			DamageType.Fire => Color.Orange,
			DamageType.Freeze => Color.Blue,
			DamageType.Poison => Color.Green,
			DamageType.Electric => Color.Cyan,
			DamageType.Radiation => Color.Magenta,
			DamageType.Acid => Color.Yellow,
			DamageType.Heal => Color.Green,
			_ => Color.White,
		};
	}

	public static string GetIcon( this DamageType type )
	{
		return type switch
		{
			DamageType.Blunt => "🏏",
			DamageType.Sharp => "⚔️",
			DamageType.Explosive => "💥",
			DamageType.Fire => "🔥",
			DamageType.Freeze => "❄️",
			DamageType.Poison => "☠️",
			DamageType.Electric => "⚡",
			DamageType.Radiation => "☢️",
			DamageType.Acid => "🧪",
			DamageType.Heal => "❤️",
			_ => "",
		};
	}
}

public interface IDamage
{
	public void OnDamage( DamageInstance damage );
}

public partial record class DamageInstance
{
	/// <summary>
	/// The damage flags associated with this event
	/// </summary>
	public DamageFlags Flags { get; set; }

	/// <summary>
	/// What's the damage type?
	/// </summary>
	public DamageType Type { get; set; }

	/// <summary>
	/// Who dunnit?
	/// </summary>
	public Component Attacker { get; set; }

	/// <summary>
	/// What caused the damage? Weapon?
	/// </summary>
	public Component Inflictor { get; set; }

	/// <summary>
	/// Who's the victim?
	/// </summary>
	public Component Victim { get; set; }

	/// <summary>
	/// How much damage?
	/// </summary>
	public float Damage { get; set; }

	/// <summary>
	/// Force
	/// </summary>
	public Vector3 Force { get; set; }

	/// <summary>
	/// Where?
	/// </summary>
	public Vector3 Position { get; set; }

	/// <summary>
	/// How long since this damage info event happened?
	/// </summary>
	public RealTimeSince TimeSince { get; init; } = 0;

	public override string ToString()
	{
		return $"\"{Attacker}\" - \"{Victim}\" with \"{Inflictor}\" ({Damage} damage)";
	}
}
