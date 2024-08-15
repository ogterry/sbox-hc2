namespace HC2;

public enum ItemCategory
{
	[Icon( "token" )]
	Resources,
	[Icon( "restaurant" )]
	Consumables,
	[Icon( "checkroom" )]
	Equipments,
	[Icon( "carpenter" )]
	Weapons,
	[Icon( "token" )]
	Armors,
	[Icon( "hardware" )]
	Tools,
	[Icon( "construction" )]
	Building,
	[Icon( "medical_services" )]
	Ammo,
	[Icon( "category" )]
	Other
}

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
	[TextArea] public string Description { get; set; }

	public ItemCategory Category { get; set; }

	/// <summary>
	/// The maximum amount that can be stacked in a single slot.
	/// </summary>
	public int MaxStack { get; set; } = 99;

	/// <summary>
	/// The maximum durability of the item.
	/// </summary>
	public float MaxDurability { get; set; } = 1f;

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
	/// What world model to use for this item.
	/// </summary>
	public Model WorldModel { get; set; }

	[Category( "Crafting" )]
	public List<Item> CraftingRequirements { get; set; }

	[Category( "Crafting" )]
	public int CraftingYield { get; set; } = 1;
	/// <summary>
	/// A list of status effects that this item applies to the player when equipped/consumed.
	/// </summary>
	[Category( "Effects" )]
	public List<StatusEffect> StatusEffects { get; set; } = new();

	public static List<IGrouping<ItemCategory, ItemAsset>> GetRecipeGroups()
	{
		var items = ResourceLibrary.GetAll<ItemAsset>();
		return items.Where( x => (x?.CraftingRequirements?.Count ?? 0) > 0 ).GroupBy( x => x.Category ).ToList();
	}
}