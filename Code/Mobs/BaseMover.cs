using System;

namespace HC2.Mobs;

#nullable enable

public abstract class BaseMover : Component
{
	[RequireComponent] public Mob Mob { get; private set; }

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
	/// How close to the target do we start slowing down.
	/// </summary>
	public float ApproachDistance => MaxSpeed / 2f;

	/// <summary>
	/// Are we closer than <see cref="ApproachDistance"/> from the target?
	/// </summary>
	public bool IsApproachingTarget => Mob.MoveTarget is { } target && (target - Transform.Position).LengthSquared < ApproachDistance * ApproachDistance;

	/// <summary>
	/// Last velocity we had.
	/// </summary>
	public Vector3 LastVelocity { get; private set; }

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		LookTowardsTarget();
	}

	protected void LookTowardsTarget()
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
		var diffRot = Rotation.Difference( Transform.Rotation, wishRot );

		if ( diffRot.Angle() > MaxTurnSpeed * Time.Delta )
		{
			diffRot *= MaxTurnSpeed * Time.Delta / diffRot.Angle();
		}

		Transform.Rotation = diffRot * Transform.Rotation;
	}

	protected Vector3 GetWishVelocity()
	{
		if ( Mob.MoveTarget is not { } target )
		{
			LastVelocity = Vector3.Zero;
			return LastVelocity;
		}

		var diff = target - Transform.Position;
		var approachDist = MaxSpeed / 2f;
		var approachDistSq = approachDist * approachDist;
		var stopDistSq = approachDistSq / 16f;

		if ( diff.LengthSquared < stopDistSq )
		{
			Mob.ClearMoveTarget();
			LastVelocity = Vector3.Zero;
			return LastVelocity;
		}

		var wishDir = diff.LengthSquared < approachDistSq
			? diff / approachDist
			: diff.Normal;

		var forwardness = MathF.Max( 0f, Vector3.Dot( wishDir, Transform.Rotation.Forward ) );

		LastVelocity = forwardness * wishDir * MaxSpeed;
		return LastVelocity;
	}
}

