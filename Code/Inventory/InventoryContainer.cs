using System;
using System.Text.Json.Serialization;

namespace HC2;

public struct InventoryContainer
{
	public List<Item> Items { get; set; } = new();
	public int MaxSlots { get; set; } = 9;
	public Inventory Inventory { get; set; }

	public InventoryContainer(Inventory inventory, int maxSlots = 9)
	{
		Inventory = inventory;
		Items = new();
		MaxSlots = maxSlots;
		for (int i = 0; i < MaxSlots; i++)
		{
			Items.Add(null);
		}
	}

	public bool TryGiveItem(Item item)
	{
		if (!Inventory.IsValid()) return false;
		for (int i = 0; i < MaxSlots; i++)
		{
			if (Items[i] == null)
			{
				Items[i] = item;
				item.Container = this;
				return true;
			}
			else if (Items[i].Resource == item.Resource)
			{
				Items[i].Amount += item.Amount;
				return true;
			}
		}
		return false;
	}

	public bool TryGiveItem(ItemAsset resource, int amount = 1)
	{
		var item = new Item();
		item.Resource = resource;
		item.Amount = amount;
		return TryGiveItem(item);
	}

	public bool TryGiveItemSlot(Item item, int slotIndex)
	{
		if (!Inventory.IsValid()) return false;
		if (slotIndex < 0 || slotIndex >= MaxSlots)
		{
			throw new ArgumentOutOfRangeException(nameof(slotIndex));
		}

		if (Items[slotIndex] != null)
		{
			if (Items[slotIndex].Resource != item.Resource)
			{
				return false;
			}
			Items[slotIndex].Amount += item.Amount;
		}
		else
		{
			Items[slotIndex] = item;
			item.Container = this;
		}

		return true;
	}

	public void TakeItem(Item item)
	{
		for (int i = MaxSlots - 1; i >= 0; i--)
		{
			if (Items[i] == item)
			{
				Items[i] = null;
				item.Container = null;
				return;
			}
			else if (Items[i].Resource == item.Resource)
			{
				Items[i].Amount -= item.Amount;
				if (Items[i].Amount <= 0)
				{
					item.Amount = Math.Abs(Items[i].Amount);
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