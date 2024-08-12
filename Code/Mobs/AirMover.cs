
using System;

namespace HC2.Mobs;

[Icon( "transfer_within_a_station" )]
public sealed class AirMover : Component
{
	[Property, Range( 50f, 500f )]
	public float MaxSpeed { get; set; } = 150f;

	/// <summary>
	/// When moving, turn to look in move dir this many degrees per second.
	/// </summary>
	[Property, Range( 0f, 360f )]
	public float MaxTurnSpeed { get; set; } = 180f;

	/// <summary>
	/// When moving, can move up to this many degrees away from forward vector.
	/// </summary>
	[Property, Range( 0f, 180f )]
	public float MaxMoveAngle { get; set; } = 45f;

	/// <summary>
	/// The minimum height above the ground the enemy should maintain.
	/// </summary>
	[Property, Range( 50f, 500f )]
	public float MinHeightAboveGround { get; set; } = 100f;

	[RequireComponent]
	public Mob Mob { get; private set; }

	[RequireComponent]
	public Rigidbody Rigidbody { get; private set; }

	public float ApproachDistance => MaxSpeed / 2f;

	public bool IsApproachingTarget => Mob.MoveTarget is { } target && (target - Transform.Position).LengthSquared < ApproachDistance * ApproachDistance;

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

	private void LookTowardsTarget()
	{
		if ( Mob.MoveTarget is not { } target )
		{
			return;
		}

		var diff = (target - Transform.Position).WithZ( 0f );

		if ( diff.LengthSquared < 16f )
		{
			// Too close!
			return;
		}

		var wishDir = diff.Normal;
		var wishRot = Rotation.LookAt( wishDir, Vector3.Up );

		Transform.Rotation = Rotation.Lerp( Transform.Rotation, wishRot, 0.2f );
	}

	private Vector3 GetWishVelocity()
	{
		if ( Mob.MoveTarget is not { } target )
		{
			return Vector3.Zero;
		}

		var diff = target - Transform.Position;
		var approachDist = MaxSpeed / 2f;
		var approachDistSq = approachDist * approachDist;
		var stopDistSq = approachDistSq / 16f;

		if ( diff.LengthSquared < stopDistSq )
		{
			Mob.ClearMoveTarget();
			return Vector3.Zero;
		}

		var wishDir = diff.LengthSquared < approachDistSq
			? diff / approachDist
			: diff.Normal;

		var forwardness = MathF.Max( 0f, Vector3.Dot( wishDir, Transform.Rotation.Forward ) );

		return forwardness * wishDir * MaxSpeed;
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

