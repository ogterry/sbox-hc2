namespace HC2;

public static class WorldPersistenceEditor
{
	/// <summary>
	/// We want to clear the target world when exiting play mode
	/// </summary>
	[Event( "scene.stop" )]
	public static void StopPlaying()
	{
		WorldPersistence.FileToLoad = null;
	}
}
