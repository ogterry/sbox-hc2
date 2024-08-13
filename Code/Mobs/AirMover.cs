
namespace HC2.Mobs;

#nullable enable

[Icon( "transfer_within_a_station" )]
public sealed class AirMover : BaseMover
{
	/// <summary>
	/// The minimum height above the ground the enemy should maintain.
	/// </summary>
	[Property, Range( 50f, 500f )]
	public float MinHeightAboveGround { get; set; } = 100f;

	[RequireComponent] public Rigidbody Rigidbody { get; private set; }

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		LookTowardsTarget();

		var wishVel = GetWishVelocity();

		AdjustHeightAboveGround( ref wishVel );

		if ( !IsApproachingTarget )
		{
			Rigidbody.Velocity = wishVel;
		}
	}

	/// <summary>
	/// Adjusts the height of the enemy to maintain a minimum height above the ground.
	/// </summary>
	private void AdjustHeightAboveGround( ref Vector3 wishVel )
	{
		// Perform a ray trace downward to detect the ground below the enemy.
		var trace = Scene.Trace.Ray( Transform.Position, Transform.Position + Vector3.Down * 1000f )
			.UsePhysicsWorld()
			.Run();

		if ( trace.Hit )
		{
			// Calculate the current height above the ground.
			float heightAboveGround = Transform.Position.z - trace.EndPosition.z;

			// If the height is less than the minimum required, adjust the vertical velocity to move upwards.
			if ( heightAboveGround < MinHeightAboveGround )
			{
				wishVel += Vector3.Up * (MinHeightAboveGround - heightAboveGround);
			}
		}
	}
}
