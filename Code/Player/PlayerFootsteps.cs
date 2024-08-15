using System;
using HC2;

public sealed class PlayerFootsteps : Component
{
	[Property] SkinnedModelRenderer Source { get; set; }
	[Property] GameObject Particle { get; set; }

	protected override void OnEnabled()
	{
		if ( Source is null )
			return;

		Source.OnFootstepEvent += OnEvent;
	}

	protected override void OnDisabled()
	{
		if ( Source is null )
			return;

		Source.OnFootstepEvent -= OnEvent;
	}

	TimeSince timeSinceStep;

	private void OnEvent( SceneModel.FootstepEvent e )
	{
		if ( timeSinceStep < 0.2f )
			return;

		var tr = Scene.Trace
			.Ray( e.Transform.Position + Vector3.Up * 20, e.Transform.Position + Vector3.Up * -20 )
			.Run();

		if ( !tr.Hit )
			return;

		if ( tr.Surface is null )
			return;

		timeSinceStep = 0;

		var sound = e.FootId == 0 ? tr.Surface.Sounds.FootLeft : tr.Surface.Sounds.FootRight;
		if ( sound is null ) return;

		var handle = Sound.Play( sound, tr.HitPosition + tr.Normal * 5 );
		handle.Volume *= e.Volume;

		OnFootstep( tr.HitPosition + tr.Normal * 5 );
	}

	public void OnFootstep( Vector3 position )
	{
		if ( Particle == null ) return;

		VoxelParticles.Spawn( position, 4 );

		// var particle = Particle.Clone();
		// particle.Transform.Position = position;

		// var effect = particle.Components.Get<ParticleEffect>( FindMode.InChildren );
		// if ( effect is null ) return;
		// effect.Tint = Color.Gray;
	}
}