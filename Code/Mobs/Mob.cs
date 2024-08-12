namespace HC2.Mobs;

#nullable enable

[Icon( "adjust" )]
public sealed class MobTarget : Component
{

}

[Icon( "mood_bad" )]
public sealed class Mob : Component
{
	public Transform SpawnTransform { get; private set; }

	public MobTarget? AimTarget { get; private set; }
	public Vector3? MoveTarget { get; private set; }

	public bool HasAimTarget => AimTarget.IsValid();
	public bool HasMoveTarget => MoveTarget is not null;

	protected override void OnStart()
	{
		SpawnTransform = Transform.World;
	}

	public void SetMoveTarget( Vector3 position )
	{
		MoveTarget = position;
	}

	public void ClearMoveTarget()
	{
		MoveTarget = null;
	}

	public void SetAimTarget( MobTarget target )
	{
		AimTarget = target;
	}

	public void ClearAimTarget()
	{
		AimTarget = null;
	}
}
