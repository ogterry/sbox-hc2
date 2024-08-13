
using System;

namespace HC2.Mobs;

#nullable enable

[Icon( "transfer_within_a_station" )]
public sealed class GroundMover : BaseMover
{
	[RequireComponent] public CharacterController CharacterController { get; private set; }

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		LookTowardsTarget();

		var wishVel = GetWishVelocity();

		if ( !CharacterController.IsOnGround )
		{
			CharacterController.Accelerate( Vector3.Down * 300f );
		}

		CharacterController.Accelerate( wishVel - CharacterController.Velocity );
		CharacterController.Move();
	}
}
