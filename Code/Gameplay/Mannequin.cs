using System;

/// <summary>
/// A shitty mannequin component that reacts to getting hit by a player. 
/// </summary>
public partial class Mannequin : Component, Component.IDamageable
{
	[Property] public GameObject Root { get; set; }
	[Property] public float Jiggle { get; set; } = 100f;
	[Property] public float JiggleRecovery { get; set; } = 0.1f;

	RealTimeSince TimeSinceAttacked { get; set; }

	Vector3 trackedPosition;
	Vector3 lastDirection;

	void IDamageable.OnDamage( in DamageInfo damage )
	{
		BroadcastAttack( ( damage.Attacker.Transform.Position - Transform.Position ).Normal ) ;

		//Should be more generic
		var dmgEffect = Components.Get<DamageEffect>();
		if ( dmgEffect != null )
		{
			dmgEffect.OnDamage( damage );
		}
	}

	[Broadcast]
	private void BroadcastAttack( Vector3 direction )
	{
		TimeSinceAttacked = 0;
		lastDirection = direction;
	}

	protected override void OnUpdate()
	{
		if ( TimeSinceAttacked > JiggleRecovery )
		{
			trackedPosition = trackedPosition.LerpTo( 0, Time.Delta * 10f );
		}
		else
		{
			trackedPosition = trackedPosition.LerpTo( -lastDirection * Jiggle, Time.Delta * 10f );

		}

		Root.Transform.Local = new Transform()
			.WithPosition( trackedPosition )
			// Inherit scale
			.WithScale( Root.Transform.Local.Scale );
	}
}
