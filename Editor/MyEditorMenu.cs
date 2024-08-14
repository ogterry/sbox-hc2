public static class MyEditorMenu
{
	[Menu( "Editor", "HC2/My Menu Option" )]
	public static void OpenMyMenu()
	{
		EditorUtility.DisplayDialog( "It worked!", "This is being called from your library's editor code!" );
	}

	[Menu( "Editor", "HC2/Move To Floor" )]
	[Shortcut( "hc2.editor.movetofloor", "END", ShortcutType.Window )]
	public static void MoveToFloor()
	{
		using var scope = SceneEditorSession.Scope();

		foreach ( var item in EditorScene.Selection )
		{
			var thing = item as GameObject;

			var origin = thing.Transform.Position + Vector3.Up * 50000f;

			var tr = thing.Scene.Trace.Ray( origin, origin + Vector3.Down * 1000000f )
				.IgnoreGameObjectHierarchy( thing )
				.Run();

			if ( tr.Hit )
			{
				thing.Transform.Position = tr.HitPosition + Vector3.Down * 100f;
			}
		}
	}
}
