namespace HC2.Mobs;

#nullable enable

public interface IMobTarget : IValid
{

}

[Icon( "mood_bad" )]
public sealed class Mob : Component
{
	public Transform SpawnTransform { get; private set; }

	public IMobTarget? AimTarget { get; private set; }
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

	public void SetAimTarget( IMobTarget target )
	{
		AimTarget = target;
	}

	public void ClearAimTarget()
	{
		AimTarget = null;
	}
}
