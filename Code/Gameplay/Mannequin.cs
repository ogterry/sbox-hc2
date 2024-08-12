using Sandbox.Events;
using System;

/// <summary>
/// A shitty mannequin component that reacts to getting hit by a player. 
/// </summary>
public partial class Mannequin : Component, 
	IGameEventHandler<DamageTakenEvent>,
	IGameEventHandler<KilledEvent>
{
	[Property] public GameObject Root { get; set; }
	[Property] public float Jiggle { get; set; } = 100f;
	[Property] public float JiggleRecovery { get; set; } = 0.1f;
	[RequireComponent] HealthComponent HealthComponent { get; set; }

	RealTimeSince TimeSinceAttacked { get; set; }

	Vector3 trackedPosition;
	Vector3 lastDirection;

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

	void IGameEventHandler<KilledEvent>.OnGameEvent( KilledEvent eventArgs )
	{
		// Reset HP back to 100
		HealthComponent.Health = 100;
	}

	void IGameEventHandler<DamageTakenEvent>.OnGameEvent( DamageTakenEvent eventArgs )
	{
		TimeSinceAttacked = 0;
		lastDirection = (eventArgs.Instance.Attacker.Transform.Position - Transform.Position).Normal;
	}
}
