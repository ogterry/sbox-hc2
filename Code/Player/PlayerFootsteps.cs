using System;

public sealed class PlayerFootsteps : Component
{
	[Property] SkinnedModelRenderer Source { get; set; }
	[Property] GameObject Particle { get; set; }
	bool LeftFoot = true;

	Player PlayerController;

	CharacterController Controller;

	TimeSince timeSinceStep;

	protected override void OnStart()
	{
		base.OnStart();

		Controller = GameObject.Components.Get<CharacterController>();
		PlayerController = GameObject.Components.Get<Player>();
	}

	float GetWalkSpeed()
	{
		float baseSpeed = 0.5f;

		float playerSpeed = Controller.Velocity.Length;

		float minSpeed = 60.0f;
		float maxSpeed = 240.0f;

		float stepInterval = MathX.Lerp( baseSpeed, 0.2f, playerSpeed.Remap( minSpeed, maxSpeed, 0.0f, 1.0f ) );
		return Math.Max( stepInterval, 0.2f ); // Minimum step interval
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( timeSinceStep < GetWalkSpeed() ) return;
		if ( Controller.Velocity.Length < 10 ) return;

		var tr = Scene.Trace
			.Ray( Transform.Position + Vector3.Up * 20, Transform.Position + Vector3.Up * -20 )
			.Run();

		if ( !tr.Hit )
			return;

		if ( tr.Surface is null )
			return;

		timeSinceStep = 0;

		var sound = LeftFoot ? tr.Surface.Sounds.FootLeft : tr.Surface.Sounds.FootRight;
		if ( sound is null ) return;

		var handle = Sound.Play( sound, tr.HitPosition + tr.Normal * 5 );
		LeftFoot = !LeftFoot;

		OnFootstep( tr.HitPosition + tr.Normal * 5 );
	}

	public void OnFootstep( Vector3 position )
	{
		if ( Particle == null ) return;

		var particle = Particle.Clone();
		particle.Transform.Position = position;

		var effect = particle.Components.Get<ParticleEffect>(FindMode.InChildren);
		if ( effect is null ) return;
		effect.Tint = Color.Gray;
	}
}
