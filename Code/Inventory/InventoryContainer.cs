using System;
using System.Text.Json.Serialization;

namespace HC2;

public struct InventoryContainer
{
	public List<Item> Items { get; set; } = new();
	public int MaxSlots { get; set; } = 9;
	public Inventory Inventory { get; set; }

	public InventoryContainer( Inventory inventory, int maxSlots = 9 )
	{
		Inventory = inventory;
		Items = new();
		MaxSlots = maxSlots;
		for ( int i = 0; i < MaxSlots; i++ )
		{
			Items.Add( null );
		}
	}

	public void GiveItem( Item item )
	{
		for ( int i = 0; i < MaxSlots; i++ )
		{
			if ( Items[i] == null )
			{
				Items[i] = item;
				item.Container = this;
				break;
			}
			else if ( Items[i].Resource == item.Resource )
			{
				Items[i].Amount += item.Amount;
				break;
			}
		}
	}

	public void GiveItem( ItemAsset resource, int amount = 1 )
	{
		var item = new Item();
		item.Resource = resource;
		item.Amount = amount;
		GiveItem( item );
	}

	public void GiveItemSlot( Item item, int slotIndex )
	{
		if ( slotIndex < 0 || slotIndex >= MaxSlots )
		{
			throw new ArgumentOutOfRangeException( nameof( slotIndex ) );
		}

		if ( Items[slotIndex] != null )
		{
			if ( Items[slotIndex].Resource != item.Resource )
			{
				throw new InvalidOperationException( "Slot already contains a different item." );
			}
			Items[slotIndex].Amount += item.Amount;
		}
		else
		{
			Items[slotIndex] = item;
			item.Container = this;
		}
	}

	public void TakeItem( Item item )
	{
		for ( int i = MaxSlots - 1; i >= 0; i-- )
		{
			if ( Items[i] == item )
			{
				Items[i] = null;
				item.Container = null;
				return;
			}
			else if ( Items[i].Resource == item.Resource )
			{
				Items[i].Amount -= item.Amount;
				if ( Items[i].Amount <= 0 )
				{
					item.Amount = Math.Abs( Items[i].Amount );
					Items[i] = null;
					item.Container = null;
				}
			}
		}
	}


	public bool IsValid()
	{
		return Inventory.IsValid();
	}
}