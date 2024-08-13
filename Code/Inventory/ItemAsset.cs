namespace HC2;

[GameResource( "Item", "item", "An item definition.", Icon = "token" )]
public class ItemAsset : GameResource
{
	/// <summary>
	/// What is the item called?
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// What does the item do?
	/// </summary>
	public string Description { get; set; }

	/// <summary>
	/// What icon should the item use?
	/// </summary>
	[ImageAssetPath]
	public string Icon { get; set; }

	/// <summary>
	/// What tags does the item have?
	/// </summary>
	public TagSet Tags { get; set; }

	/// <summary>
	/// What prefab to spawn on the player when this item is used or equipped.
	/// </summary>
	public PrefabFile Prefab { get; set; }

	/// <summary>
	/// What prefab to spawn when creating the item itself. If empty it will create an empty GameObject
	/// with an Item component.
	/// </summary>
	public PrefabFile ItemPrefab { get; set; }

	/// <summary>
	/// What world model to use for this item.
	/// </summary>
	public Model WorldModel { get; set; }

	/// <summary>
	/// A list of status effects that this item applies to the player when equipped/consumed.
	/// </summary>
	List<StatusEffect> StatusEffects { get; set; } = new();
}
