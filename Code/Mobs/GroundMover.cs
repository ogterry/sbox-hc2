
namespace HC2.Mobs;

[Icon( "transfer_within_a_station" )]
public sealed class GroundMover : Component
{
	[Property, Range( 50f, 500f )]
	public float MaxSpeed { get; set; } = 150f;

	[RequireComponent]
	public Mob Mob { get; private set; }

	[RequireComponent]
	public CharacterController CharacterController { get; private set; }

	public float ApproachDistance => MaxSpeed / 2f;

	public bool IsApproachingTarget => Mob.MoveTarget is { } target && (target - Transform.Position).LengthSquared < ApproachDistance * ApproachDistance;

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		var wishVel = GetWishVelocity();

		CharacterController.Accelerate( wishVel - CharacterController.Velocity);
		CharacterController.Move();
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
		var stopDistSq = approachDistSq * 16f;

		if ( diff.LengthSquared < stopDistSq )
		{
			Mob.ClearMoveTarget();
			return Vector3.Zero;
		}

		var wishDir = diff.LengthSquared < approachDistSq
			? diff / approachDist
			: diff.Normal;

		return wishDir * MaxSpeed;
	}
}

