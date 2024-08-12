using Sandbox;
public sealed class DamageEffect : Component
{
	[Property] GameObject Particle { get; set; }

	public void OnDamage( in DamageInstance damage )
	{
		if ( Particle == null ) return;

		var particle = Particle.Clone();
		particle.Transform.Position = damage.Position;

		var textRenderer = particle.Components.Get<ParticleTextRenderer>();
		if ( textRenderer != null )
		{
			textRenderer.Text = new TextRendering.Scope( damage.Type.GetIcon() + damage.Damage.ToString(), damage.Type.GetColor(), 32, weight: 800 );
		}
	}
}
