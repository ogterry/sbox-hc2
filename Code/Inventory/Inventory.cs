using System;
using Sandbox.Diagnostics;

namespace HC2;

[Category( "Inventory" )]
[Description( "An inventory that should contain slots for items." )]
[Icon( "inventory_2" )]
public class Inventory : Component
{
	/// <summary>
	/// Get all the <see cref="InventorySlot"/> slots we have available.
	/// </summary>
	public List<InventorySlot> Slots { get; private set; } = new();

	/// <summary>
	/// Create a new networked inventory.
	/// </summary>
	/// <returns></returns>
	public static Inventory Create( int slots, Connection owner = null )
	{
		Assert.True( Sandbox.Networking.IsHost );
		
		var go = new GameObject();
		var inventory = go.Components.Create<Inventory>();

		for ( var i = 0; i < 8; i++ )
		{
			var slotGo = new GameObject();
			slotGo.Components.Create<InventorySlot>();
			slotGo.Parent = go;
		}
		
		go.NetworkSpawn( owner );
		
		return inventory;
	}

	protected override void OnRefresh()
	{
		UpdateSlots();
		base.OnRefresh();
	}

	protected override void OnStart()
	{
		UpdateSlots();
		base.OnStart();
	}

	void UpdateSlots()
	{
		Slots.Clear();
		
		for ( var i = 0; i < GameObject.Children.Count; i++ )
		{
			var slot = GameObject.Children[i].Components.Get<InventorySlot>();
			if ( !slot.IsValid() ) continue;

			slot.SlotIndex = Slots.Count;
			Slots.Add( slot );
		}
	}
	
	/// <summary>
	/// Get an item in the specified slot.
	/// </summary>
	/// <param name="slot"></param>
	/// <returns></returns>
	public Item GetItemInSlot( int slot )
	{
		if ( slot < 0 || slot >= Slots.Count )
		{
			throw new ArgumentOutOfRangeException( nameof( slot ) );
		}

		var child = Slots[slot];
		return child.IsValid() ? child.Components.GetInChildren<Item>() : null;
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
		var oldSlot = item.Slot;
		var slot = GetSlotAt( slotIndex );
		var otherItem = slot.Item;

		if ( otherItem.IsValid() )
		{
			if ( oldInventory.IsValid() && oldSlot.IsValid() )
			{
				otherItem.GameObject.SetParent( oldSlot.GameObject );
				
				// Don't refresh if its us because we're gonna refresh later.
				if ( oldInventory != this )
					oldInventory.Network.Refresh();
			}
			else
			{
				Log.Warning( "Can't move an item here as something else is already in its place and we can't swap!" );
				return;
			}
		}

		item.GameObject.SetParent( slot.GameObject );
		Network.Refresh();
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
		
		var slot = GetFreeSlot();

		if ( !slot.IsValid() )
		{
			Log.Warning( "Unable to give item to inventory because there are no free slots!" );
			return;
		}

		var oldContainer = item.Container;
		item.GameObject.SetParent( slot.GameObject );
		Network.Refresh();
		
		if ( oldContainer.IsValid() )
			oldContainer.Network.Refresh();
	}

	/// <summary>
	/// Get the <see cref="InventorySlot"/> at the specified index.
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public InventorySlot GetSlotAt( int index )
	{
		if ( index < 0 || index >= Slots.Count )
		{
			throw new ArgumentOutOfRangeException( nameof( index ) );
		}

		return Slots[index];
	}

	/// <summary>
	/// Get the next free slot index.
	/// </summary>
	/// <returns>Returns -1 if no slot index is free.</returns>
	public int GetFreeSlotIndex()
	{
		for ( var i = 0; i < Slots.Count; i++ )
		{
			var slot = Slots[i];
			if ( !slot.Item.IsValid() )
				return i;
		}

		return -1;
	}

	/// <summary>
	/// Get the next free <see cref="InventorySlot"/>.
	/// </summary>
	/// <returns></returns>
	public InventorySlot GetFreeSlot()
	{
		foreach ( var slot in Slots )
		{
			if ( !slot.Item.IsValid() )
			{
				return slot;
			}
		}

		return null;
	}

	/// <summary>
	/// Get all items contained in this inventory.
	/// </summary>
	/// <returns></returns>
	public IEnumerable<Item> GetAllItems()
	{
		for ( var i = 0; i < Slots.Count; i++ )
		{
			var item = Slots[i].Item;
			if ( item.IsValid() )
				yield return item;
		}
	}
}
