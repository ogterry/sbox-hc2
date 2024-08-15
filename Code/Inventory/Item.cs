using System.Text.Json.Serialization;
using Voxel;

namespace HC2;

public class Item : IValid
{
	/// <summary>
	/// Get the item's resource asset.
	/// </summary>
	[KeyProperty] public ItemAsset Resource { get; init; }

	/// <summary>
	/// What is the current stack size of this item?
	/// </summary>
	[KeyProperty] public int Amount { get; set; } = 1;

	/// <summary>
	/// What is this item's current durability?
	/// </summary>
	public float Durability { get; set; } = 1f;

	[JsonIgnore, Hide] public Inventory Container;
	[JsonIgnore, Hide] public int SlotIndex => Container?.Items.IndexOf( this ) ?? -1;

	/// <summary>
	/// Is this item valid? Does it have a valid resource and inventory container.
	/// </summary>
	public bool IsValid => Resource != null && (Container?.IsValid() ?? false);

	/// <summary>
	/// Get the name of this item.
	/// </summary>
	[JsonIgnore, Hide] public virtual string Name => Resource?.Name ?? string.Empty;
	
	/// <summary>
	/// Get the max stack size for this item according to its resource asset.
	/// </summary>
	[JsonIgnore, Hide] public virtual int MaxStack => Resource?.MaxStack ?? 1;

	public static Item Create( ItemAsset resource, int amount = 1 )
	{
		var item = new Item { Resource = resource, Amount = amount, Durability = resource.MaxDurability };
		return item;
	}

	public static Item Create( string resourceName, int amount = 1 )
	{
		var resource = ResourceLibrary.GetAll<ItemAsset>().FirstOrDefault( x => x.ResourceName == resourceName );
		if ( resource is null )
		{
			Log.Warning( $"Couldn't find item resource {resourceName}" );
			return null;
		}

		var item = new Item
		{
			Resource = resource,
			Amount = amount,
			Durability = resource.MaxDurability
		};

		return item;
	}
}
