using Sandbox;
public sealed class DamageEffect : Component
{
	[Property] GameObject Particle { get; set; }
	[Property] DamageType.Type Type { get; set; } = DamageType.Type.Blunt;


	public void OnDamage( in DamageInfo damage )
	{
		if ( Particle == null ) return;

		var particle = Particle.Clone();
		particle.Transform.Position = damage.Position;

		var textRenderer = particle.Components.Get<ParticleTextRenderer>();
		if ( textRenderer != null )
		{
			textRenderer.Text = new TextRendering.Scope( DamageType.GetDamageIcon( Type ) + damage.Damage.ToString(), DamageType.GetColor( Type ), 32, weight: 800 );
		}

	}

	protected override void OnUpdate()
	{

	}
}

//This should probably be somewhere better?

public class DamageType
{
	public enum Type
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

	public static Color GetColor( Type type )
	{
		return type switch
		{
			Type.Blunt => Color.White,
			Type.Sharp => Color.Red,
			Type.Explosive => Color.Yellow,
			Type.Fire => Color.Orange,
			Type.Freeze => Color.Blue,
			Type.Poison => Color.Green,
			Type.Electric => Color.Cyan,
			Type.Radiation => Color.Magenta,
			Type.Acid => Color.Yellow,
			Type.Heal => Color.Green,
			_ => Color.White,
		};
	}

	public static string GetDamageIcon ( Type type )
	{
		return type switch
		{
			Type.Blunt => "🏏",
			Type.Sharp => "⚔️",
			Type.Explosive => "💥",
			Type.Fire => "🔥",
			Type.Freeze => "❄️",
			Type.Poison => "☠️",
			Type.Electric => "⚡",
			Type.Radiation => "☢️",
			Type.Acid => "🧪",
			Type.Heal => "❤️",
			_ => "",
		};
	}


}
