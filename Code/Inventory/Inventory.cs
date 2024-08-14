using System;
using Sandbox;
using Sandbox.Diagnostics;

namespace HC2;

[Category( "Inventory" )]
[Description( "An inventory that should contain slots for items." )]
[Icon( "inventory_2" )]
public class Inventory : Component, ISaveData
{
	[Property] public int MaxSlots { get; set; } = 9;

	/// <summary>
	/// The Container that this Inventory references/networks
	/// </summary>
	[Sync] public InventoryContainer Container { get; private set; }

	/// <summary>
	/// If set, if this inventory can't hold any more items, it will overflow into this container.
	/// </summary>
	[Sync] public InventoryContainer OverflowContainer { get; set; }

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
		else if ( OverflowContainer.IsValid() && OverflowContainer.CanGiveItem( item ) )
		{
			OverflowContainer.TryGiveItem( item );
			return true;
		}

		return false;
	}

	public bool TryGiveItemSlot( Item item, int slotIndex )
	{
		if ( !Container.Inventory.IsValid() )
			return false;

		if ( Container.TryGiveItemSlot( item, slotIndex ) )
		{
			return true;
		}
		else if ( OverflowContainer.IsValid() && OverflowContainer.CanGiveItem( item ) )
		{
			OverflowContainer.TryGiveItem( item );
			return true;
		}

		return false;
	}

	public bool HasItem( Item item )
	{
		return Container.HasItem( item ) || OverflowContainer.HasItem( item );
	}

	public void DropItem( int slotIndex )
	{
		var item = GetItemInSlot( slotIndex );

		if ( item?.IsValid() ?? false )
		{
			Container.ClearItemSlot( slotIndex );

			var aimRay = Player.Local.CameraController.AimRay;
			WorldItem.CreateInstance( item.Resource, aimRay.Position + aimRay.Forward * 128f, item.Amount );
		}
	}

	/// <summary>
	/// Move an item to the specified slot.
	/// </summary>
	/// <param name="item">This item must already exist on the host.</param>
	/// <param name="slotIndex"></param>
	public void MoveItem( Item item, int slotIndex )
	{
		if ( item.Container.Value == Container && slotIndex == item.SlotIndex )
		{
			return;
		}
		var itemResource = item.Resource;
		var otherInventory = item.Container;
		var oldIndex = item.SlotIndex;
		var amount = item.Amount;
		var currentItemInSlot = GetItemInSlot( slotIndex );

		otherInventory?.ClearItemSlot( oldIndex );

		if ( currentItemInSlot is not null )
		{
			if ( otherInventory is not null )
			{
				if ( currentItemInSlot?.Resource != item.Resource )
				{
					otherInventory?.TryGiveItemSlot( currentItemInSlot, oldIndex );
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
					else
					{
						currentItemInSlot.Amount += item.Amount;
						item.Amount = 0;
					}
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
	/// Give an item to this inventory.
	/// </summary>
	/// <param name="item">This item must already exist on the host.</param>
	[Authority]
	public void GiveItem( Item item )
	{
		var oldContainer = item.Container;
		oldContainer?.TakeItem( item );
		if ( oldContainer?.Inventory?.IsValid() ?? false )
		{
			oldContainer.Value.Inventory.Container = oldContainer.Value;
		}

		if ( Container.TryGiveItem( item ) )
		{
			Container = Container; // Refresh the container
		}
		else if ( OverflowContainer.TryGiveItem( item ) )
		{
			OverflowContainer.Inventory.Container = OverflowContainer;
		}
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
				itemString += $"{item.Resource.ResourceName}:{item.Amount},";
		}
		if ( itemString.EndsWith( "," ) )
			itemString = itemString.Substring( 0, itemString.Length - 1 );

		return itemString;
	}

	public void Load( string data )
	{
		var allItems = ResourceLibrary.GetAll<ItemAsset>();
		var items = data.Split( ',' );
		int i = 0;
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
			var it = Item.Create( itemResource, itemAmount );
			TryGiveItemSlot( it, i - 1 );
		}
	}
}
