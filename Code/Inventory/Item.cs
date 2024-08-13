using System;
using System.Text.Json.Serialization;
using Sandbox.Diagnostics;

namespace HC2;

public class Item
{
	public ItemAsset Resource { get; set; }
	public int Amount { get; set; } = 1;
	public float Durability { get; set; } = 1f;

	[JsonIgnore, Hide] public InventoryContainer? Container;
	[JsonIgnore, Hide] public int SlotIndex => Container?.Items?.IndexOf( this ) ?? -1;

	public static Item Create( string resourceName, int amount = 1 )
	{
		var item = new Item();
		item.Resource = ResourceLibrary.GetAll<ItemAsset>().FirstOrDefault( x => x.ResourceName == resourceName );
		item.Amount = amount;
		return item;
	}

	public bool IsValid()
	{
		return Resource != null && (Container?.IsValid() ?? false);
	}
}
