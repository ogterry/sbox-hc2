using System;
using Sandbox.Diagnostics;

namespace HC2;

[Category( "Inventory" )]
[Description( "An inventory that should contain slots for items." )]
[Icon( "inventory_2" )]
public class Inventory : Component
{
	[Property] public int MaxSlots { get; set; } = 9;

	/// <summary>
	/// The Container that this Inventory references/networks
	/// </summary>
	[Sync] public InventoryContainer Container { get; private set; }

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
	/// Move an item to the specified slot.
	/// </summary>
	/// <param name="item">This item must already exist on the host.</param>
	/// <param name="slotIndex"></param>
	[Broadcast]
	public void MoveItem( Item item, int slotIndex )
	{
		if ( !Sandbox.Networking.IsHost )
			return;

		var oldInventory = item.Container;
		var otherItem = GetItemInSlot( slotIndex );

		if ( otherItem.IsValid() )
		{
			if ( oldInventory?.IsValid() ?? false )
			{
				oldInventory?.TakeItem( item );
				oldInventory.Value.Inventory.Container = oldInventory.Value;
			}
			else
			{
				Log.Warning( "Can't move an item here as something else is already in its place and we can't swap!" );
				return;
			}
		}

		Container.GiveItemSlot( item, slotIndex );
		Container = Container; // Refresh the container
	}

	/// <summary>
	/// Give an item to this inventory.
	/// </summary>
	/// <param name="item">This item must already exist on the host.</param>
	[Broadcast]
	public void GiveItem( Item item )
	{
		if ( !Sandbox.Networking.IsHost )
			return;

		var slot = GetFreeSlotIndex();

		if ( slot == -1 )
		{
			Log.Warning( "Unable to give item to inventory because there are no free slots!" );
			return;
		}

		var oldContainer = item.Container;
		oldContainer?.TakeItem( item );
		if ( oldContainer?.Inventory?.IsValid() ?? false )
		{
			oldContainer.Value.Inventory.Container = oldContainer.Value;
		}

		Container.GiveItemSlot( item, slot );
		Container = Container; // Refresh the container
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
}
