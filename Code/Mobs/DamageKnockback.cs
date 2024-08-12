
using Sandbox.Events;

namespace HC2.Mobs;

[Icon( "personal_injury" )]
public sealed class DamageKnockback : Component,
	IGameEventHandler<DamageTakenEvent>
{
	[RequireComponent]
	public CharacterController CharacterController { get; private set; }

	[Property, Range( 0f, 4f )]
	public float Scale { get; set; } = 1f;

	[Property]
	public float Mass { get; set; } = 10f;

	void IGameEventHandler<DamageTakenEvent>.OnGameEvent( DamageTakenEvent eventArgs )
	{
		CharacterController.Velocity += eventArgs.Instance.Force / Mass * Scale;
	}
}
