using Sandbox;

public sealed class Destroyer : Component
{
	[Property] float Time { get; set; } = 2.0f;
	TimeSince timeSinceCreation;

	protected override void OnStart()
	{
		base.OnStart();
		timeSinceCreation = 0;
	}

	protected override void OnUpdate()
	{
		if ( timeSinceCreation > Time )
		{
			GameObject.Destroy();
		}
	}
}
