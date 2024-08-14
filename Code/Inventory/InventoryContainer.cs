using System;
using System.Text.Json.Serialization;

namespace HC2;

public struct InventoryContainer
{
	public List<Item> Items { get; set; } = new();
	public int MaxSlots { get; set; } = 9;
	public Inventory Inventory { get; set; }
	
	private Guid Id { get; } = Guid.NewGuid();

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

	public bool CanGiveItem( Item item )
	{
		if ( !Inventory.IsValid() ) return false;
		
		for ( var i = 0; i < MaxSlots; i++ )
		{
			if ( Items[i] == null )
				return true;

			if ( Items[i].Resource == item.Resource )
				return true;
		}
		
		return false;
	}

	public bool HasItem( Item item )
	{
		if ( !Inventory.IsValid() ) return false;
		
		var amount = item.Amount;
		
		for ( var i = 0; i < MaxSlots; i++ )
		{
			if ( Items[i]?.Resource != item.Resource )
				continue;

			amount -= Items[i].Amount;
			
			if ( amount <= 0 )
				return true;
		}
		
		return false;
	}
	/// <summary>
	/// <inheritdoc cref="Inventory.ClearItemSlot"/>
	/// </summary>
	public void ClearItemSlot( int slotIndex )
	{
		if ( !Inventory.IsValid() ) return;
		
		if ( slotIndex < 0 || slotIndex >= MaxSlots )
		{
			throw new ArgumentOutOfRangeException( nameof( slotIndex ) );
		}

		Items[slotIndex] = null;
	}

	/// <summary>
	/// <inheritdoc cref="Inventory.TryGiveItem"/>
	/// </summary>
	public bool TryGiveItem( Item item )
	{
		if ( !Inventory.IsValid() ) return false;
		
		var amount = item.Amount;
		
		for ( var i = 0; i < MaxSlots; i++ )
		{
			if ( Items[i] == null )
			{
				Items[i] = item;
				item.Container = Inventory;
				
				if ( amount > item.Resource.MaxStack )
				{
					Items[i].Amount = item.Resource.MaxStack;
					amount -= item.Resource.MaxStack;
				}
				else
				{
					return true;
				}
			}
			else if ( Items[i].Resource == item.Resource )
			{
				if ( Items[i].Amount + item.Amount > item.Resource.MaxStack )
				{
					amount -= item.Resource.MaxStack - Items[i].Amount;
					Items[i].Amount = item.Resource.MaxStack;
				}
				else
				{
					Items[i].Amount += item.Amount;
					return true;
				}
			}
		}
		
		return false;
	}

	/// <summary>
	/// <inheritdoc cref="Inventory.TryGiveItem"/>
	/// </summary>
	public bool TryGiveItem( ItemAsset resource, int amount = 1 )
	{
		var item = new Item { Resource = resource, Amount = amount };
		return TryGiveItem( item );
	}

	/// <summary>
	/// <inheritdoc cref="Inventory.TryGiveItemSlot"/>
	/// </summary>
	public bool TryGiveItemSlot( Item item, int slotIndex )
	{
		if ( !Inventory.IsValid() ) return false;
		if ( slotIndex < 0 || slotIndex >= MaxSlots )
		{
			throw new ArgumentOutOfRangeException( nameof( slotIndex ) );
		}

		if ( Items[slotIndex] != null )
		{
			if ( Items[slotIndex].Resource != item.Resource )
			{
				return false;
			}

			if ( Items[slotIndex].Amount + item.Amount > item.Resource.MaxStack )
			{
				var leftover = Items[slotIndex].Amount + item.Amount - item.Resource.MaxStack;
				Items[slotIndex].Amount = item.Resource.MaxStack;
				TryGiveItem( Item.Create( item.Resource, leftover ) );
			}
			else
			{
				Items[slotIndex].Amount += item.Amount;
				return true;
			}
		}
		else
		{
			Items[slotIndex] = item;
			item.Container = Inventory;
		}

		return true;
	}

	/// <summary>
	/// <inheritdoc cref="Inventory.TakeItem"/>
	/// </summary>
	public void TakeItem( Item item )
	{
		for ( var i = MaxSlots - 1; i >= 0; i-- )
		{
			if ( Items[i] == item )
			{
				Items[i] = null;
				item.Container = null;
				return;
			}

			if ( Items[i].Resource == item.Resource )
			{
				Items[i].Amount -= item.Amount;
				
				if ( Items[i].Amount > 0 )
					continue;

				item.Amount = Math.Abs( Items[i].Amount );
				Items[i] = null;
				item.Container = null;
			}
		}
	}

	/// <summary>
	/// <inheritdoc cref="Inventory.GetItemInSlot"/>
	/// </summary>
	public Item GetItemInSlot( int slot )
	{
		if ( slot < 0 || slot >= MaxSlots )
		{
			throw new ArgumentOutOfRangeException( nameof( slot ) );
		}

		var item = Items[slot];
		return item;
	}
	
	public bool IsValid()
	{
		return Inventory.IsValid();
	}

	public static bool operator ==( InventoryContainer a, InventoryContainer b )
	{
		return a.Id == b.Id;
	}

	public static bool operator !=( InventoryContainer a, InventoryContainer b )
	{
		return a.Id != b.Id;
	}

	public override bool Equals( object obj )
	{
		return obj is InventoryContainer container && Id == container.Id;
	}

	public override int GetHashCode()
	{
		return Id.GetHashCode();
	}
}
