using System;
using Sandbox.Diagnostics;

namespace HC2;

[Hide]
[Category( "Inventory" )]
public class Item : Component
{
	[Property] public ItemAsset Resource { get; set; }

	/// <summary>
	/// Get the <see cref="InventorySlot"/> this item is in.
	/// </summary>
	public InventorySlot Slot => Components.GetInParent<InventorySlot>();

	/// <summary>
	/// Get the <see cref="Inventory"/> this item is contained in.
	/// </summary>
	public Inventory Container => Slot?.Container;
	
	/// <summary>
	/// Create a new item from its asset.
	/// </summary>
	/// <param name="asset"></param>
	/// <returns></returns>
	public static Item Create( ItemAsset asset )
	{
		Assert.True( Sandbox.Networking.IsHost );
		
		GameObject go;
		Item item;
		
		if ( asset.ItemPrefab is not null )
		{
			go = GameObject.Clone( asset.ItemPrefab );
			item = go.Components.Get<Item>();
		}
		else
		{
			go = new();
			item = go.Components.Create<Item>();
		}
		
		item.Resource = asset;
		return item;
	}
	
	/// <summary>
	/// Create a new item from its asset name.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public static Item Create( string name )
	{
		Assert.True( Sandbox.Networking.IsHost );
		
		var asset = ResourceLibrary.GetAll<ItemAsset>()
			.FirstOrDefault( i => string.Equals( i.ResourceName, name, StringComparison.CurrentCultureIgnoreCase ) );

		return asset is null ? null : Create( asset );
	}

	[Broadcast]
	public void Remove()
	{
		if ( !Sandbox.Networking.IsHost )
			return;
		
		Destroy();
		
		var container = Container;
		if ( container.IsValid() )
			container.Network.Refresh();
	}
}
