namespace HC2;

/// <summary>
/// A temporary component that outputs the prefab source to a string.. This sucks. And we should fix this ASAP.
/// </summary>
public partial class GetPrefabSource : Component
{
	public string PrefabSource { get; set; }

	protected override void OnAwake()
	{
		PrefabSource = GameObject.PrefabInstanceSource;
	}
}
