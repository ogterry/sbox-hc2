using System;
using System.Text.Json.Serialization;
using Sandbox.Diagnostics;

namespace HC2;

public class Item
{
	[KeyProperty] public ItemAsset Resource { get; init; }
	[KeyProperty] public int Amount { get; set; } = 1;
	public float Durability { get; set; } = 1f;

	[JsonIgnore, Hide] public Inventory Container;
	[JsonIgnore, Hide] public int SlotIndex => Container?.Items.IndexOf( this ) ?? -1;

	public static Item Create( ItemAsset resource, int amount = 1 )
	{
		var item = new Item { Resource = resource, Amount = amount };
		return item;
	}

	public static Item Create( string resourceName, int amount = 1 )
	{
		var item = new Item
		{
			Resource = ResourceLibrary.GetAll<ItemAsset>().FirstOrDefault( x => x.ResourceName == resourceName ),
			Amount = amount
		};
		
		return item;
	}

	/// <summary>
	/// Is this item valid? Does it have a valid resource and inventory container.
	/// </summary>
	/// <returns></returns>
	public bool IsValid()
	{
		return Resource != null && (Container?.IsValid() ?? false);
	}
}
