using System;

namespace HC2;

[Category( "Inventory" )]
[Description( "An inventory that should contain slots for items." )]
[Icon( "inventory_2" )]
public class Inventory : Component, ISaveData
{
	[Property] public int MaxSlots { get; set; } = 9;

	/// <summary>
	/// Get the items this Inventory holds.
	/// </summary>
	public List<Item> Items => Container.Items;

	/// <summary>
	/// The container that this Inventory references.
	/// </summary>
	public InventoryContainer Container { get; private set; }

	/// <summary>
	/// If set and this inventory can't hold any more items, it will overflow into this container.
	/// </summary>
	public InventoryContainer OverflowContainer { get; set; }

	public List<Item> CombinedItems => Container.Items.Concat( OverflowContainer.Items ?? new List<Item>() ).ToList();

	protected override void OnAwake()
	{
		Container = new( this, MaxSlots );
	}

	/// <summary>
	/// Get an item in the specified slot.
	/// </summary>
	/// <param name="slot"></param>
	/// <returns></returns>
	public Item GetItemInSlot( int slot )
	{
		if ( slot < 0 || slot >= MaxSlots )
		{
			throw new ArgumentOutOfRangeException( nameof( slot ) );
		}

		var item = Container.Items[slot];
		return item;
	}

	/// <summary>
	/// Can this inventory be given the specified item?
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public bool CanGiveItem( Item item )
	{
		return Container.CanGiveItem( item ) || (OverflowContainer.IsValid() && OverflowContainer.CanGiveItem( item ));
	}

	/// <summary>
	/// Can this inventory craft the specified item?
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public bool CanCraftItem( Item item )
	{
		return CanCraftItem( item.Resource, item.Amount );
	}

	/// <summary>
	/// Can this inventory craft the specified item?
	/// </summary>
	/// <param name="item"></param>
	/// <param name="amount"></param>
	/// <returns></returns>
	public bool CanCraftItem( ItemAsset item, int amount = 1 )
	{
		if ( (item?.CraftingRequirements?.Count ?? 0) == 0 )
			return false;

		var requirements = item.CraftingRequirements;
		foreach ( var requirement in requirements )
		{
			if ( !HasItem( Item.Create( requirement.Resource, requirement.Amount * amount ) ) )
				return false;
		}

		return true;
	}

	public void CraftItem( Item item )
	{
		if ( !CanCraftItem( item ) )
			return;

		foreach ( var requirement in item.Resource.CraftingRequirements )
		{
			var reqItem = Item.Create( requirement.Resource, requirement.Amount * item.Amount );
			TakeItem( reqItem );
		}

		TryGiveItem( Item.Create( item.Resource, item.Amount * item.Resource.CraftingYield ) );
	}

	/// <summary>
	/// Try to give an item to this inventory. This can only be performed as the owner as it requires network authority.
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public bool TryGiveItem( Item item )
	{
		if ( !Container.Inventory.IsValid() )
			return false;

		if ( Container.CanGiveItem( item ) )
		{
			Container.TryGiveItem( item );
			return true;
		}

		if ( !OverflowContainer.IsValid() || !OverflowContainer.CanGiveItem( item ) )
			return false;

		OverflowContainer.TryGiveItem( item );
		return true;
	}

	/// <summary>
	/// Take the specified item from the inventory.
	/// </summary>
	/// <param name="item"></param>
	public void TakeItem( Item item )
	{
		if ( !Container.TryTakeItem( item ) )
		{
			OverflowContainer.TryTakeItem( item );
		}
	}

	/// <summary>
	/// Take the item in the specified slot.
	/// </summary>
	/// <param name="slot"></param>
	public void TakeItemSlot( int slot )
	{
		var item = GetItemInSlot( slot );
		if ( item is not null )
		{
			TakeItem( item );
		}
	}

	/// <summary>
	/// Try to give an item to the specified slot.
	/// </summary>
	/// <param name="item"></param>
	/// <param name="slotIndex"></param>
	/// <returns></returns>
	public bool TryGiveItemSlot( Item item, int slotIndex )
	{
		if ( !Container.Inventory.IsValid() )
			return false;

		if ( Container.TryGiveItemSlot( item, slotIndex ) )
		{
			return true;
		}

		if ( !OverflowContainer.IsValid() || !OverflowContainer.CanGiveItem( item ) )
			return false;

		OverflowContainer.TryGiveItem( item );
		return true;

	}

	/// <summary>
	/// Does this inventory have the specified item?
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public bool HasItem( Item item )
	{
		var items = CombinedItems;
		var amount = item.Amount;

		foreach ( var i in items )
		{
			if ( i is null ) continue;
			if ( i.Resource == item.Resource )
			{
				amount -= i.Amount;

				if ( amount <= 0 )
					return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Drop the item in the specified slot.
	/// </summary>
	/// <param name="slotIndex"></param>
	public void DropItem( int slotIndex )
	{
		var item = GetItemInSlot( slotIndex );

		if ( item?.IsValid() ?? false )
		{
			Container.ClearItemSlot( slotIndex );

			var aimRay = Player.Local.CameraController.AimRay;
			WorldItem.CreateInstance( item, aimRay.Position + aimRay.Forward * 128f );
		}
	}

	/// <summary>
	/// Move an item to the specified slot.
	/// </summary>
	/// <param name="item">This item must already exist on the host.</param>
	/// <param name="slotIndex"></param>
	public void MoveItem( Item item, int slotIndex )
	{
		if ( item.Container == this && slotIndex == item.SlotIndex )
			return;

		var oldInventory = item.Container;
		var oldIndex = item.SlotIndex;
		var currentItemInSlot = GetItemInSlot( slotIndex );

		oldInventory?.ClearItemSlot( oldIndex );

		if ( currentItemInSlot is not null )
		{
			if ( oldInventory is not null )
			{
				if ( currentItemInSlot?.Resource != item.Resource )
				{
					oldInventory?.TryGiveItemSlot( currentItemInSlot, oldIndex );
					Container.ClearItemSlot( slotIndex );
				}
				else
				{
					if ( currentItemInSlot.Amount + item.Amount > currentItemInSlot.Resource.MaxStack )
					{
						var diff = currentItemInSlot.Resource.MaxStack - currentItemInSlot.Amount;
						currentItemInSlot.Amount = currentItemInSlot.Resource.MaxStack;
						TryGiveItem( Item.Create( item.Resource, item.Amount - diff ) );

						return;
					}

					currentItemInSlot.Amount += item.Amount;
					item.Amount = 0;
				}
			}
			else
			{
				Log.Warning( "Can't move an item here as something else is already in its place and we can't swap!" );
				return;
			}
		}

		Container.TryGiveItemSlot( item, slotIndex );
	}

	/// <summary>
	/// Clear the item in the specified slot.
	/// </summary>
	/// <param name="slotIndex"></param>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public void ClearItemSlot( int slotIndex )
	{
		if ( slotIndex < 0 || slotIndex >= MaxSlots )
		{
			throw new ArgumentOutOfRangeException( nameof( slotIndex ) );
		}

		Items[slotIndex] = null;
	}

	/// <summary>
	/// Give an item to this inventory.
	/// </summary>
	/// <param name="item">This item must already exist on the host.</param>
	public void GiveItem( Item item )
	{
		var oldContainer = item.Container;
		oldContainer?.TakeItem( item );

		if ( Container.TryGiveItem( item ) )
			return;

		if ( OverflowContainer.IsValid() )
			OverflowContainer.TryGiveItem( item );
	}

	/// <summary>
	/// Get the next free slot index.
	/// </summary>
	/// <returns>Returns -1 if no slot index is free.</returns>
	public int GetFreeSlotIndex()
	{
		for ( var i = 0; i < MaxSlots; i++ )
		{
			if ( Container.Items[i] is null )
				return i;
		}

		return -1;
	}

	/// <summary>
	/// Get all items contained in this inventory.
	/// </summary>
	/// <returns></returns>
	public IEnumerable<Item> GetAllItems()
	{
		for ( var i = 0; i < MaxSlots; i++ )
		{
			var item = Container.Items[i];

			if ( item is not null )
				yield return item;
		}
	}

	public string Save()
	{
		var itemString = "";

		foreach ( var item in Container.Items )
		{
			if ( item is null )
				itemString += "null,";
			else
				itemString += $"{item.Resource.ResourceName}:{item.Amount}:{item.Durability},";
		}

		if ( itemString.EndsWith( "," ) )
			itemString = itemString.Substring( 0, itemString.Length - 1 );

		return itemString;
	}

	public void Load( string data )
	{
		var allItems = ResourceLibrary.GetAll<ItemAsset>();
		var items = data.Split( ',' );
		var i = 0;

		foreach ( var item in items )
		{
			i++;

			if ( item == "null" )
				continue;

			var itemData = item.Split( ':' );
			var itemResource = allItems.FirstOrDefault( x => x.ResourceName == itemData[0] );

			if ( itemResource is null )
			{
				Log.Warning( $"Couldn't find item resource {itemData[0]}" );
				continue;
			}

			var itemAmount = int.Parse( itemData[1] );
			var itemDurability = 1f;
			if ( itemData.Length > 2 )
				itemDurability = float.Parse( itemData[2] );
			var it = Item.Create( itemResource, itemAmount );
			it.Durability = itemDurability;

			TryGiveItemSlot( it, i - 1 );
		}
	}
}
