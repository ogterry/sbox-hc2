using HC2;
using Sandbox.Events;

public partial class Player
{
    [ConCmd( "hc2_listitems" )]
    public static void ListPlayersAndItems()
    {
        var players = Game.ActiveScene.GetAllComponents<Player>();

        foreach ( var p in players )
        {
            Log.Info( "==" + p.Network.OwnerConnection.DisplayName );
            foreach ( var item in p.Inventory.GetAllItems() )
            {
                Log.Info( item.Resource.Name + " / " + item.SlotIndex );
            }
        }
    }

    [ConCmd( "hc2_giveitem" )]
    public static void GiveLocalPlayerItem( string itemName, int amount = 1 )
    {
        if ( !Game.IsEditor ) return;
        var item = Item.Create( itemName, amount );
        if ( item is null )
        {
            Log.Warning( $"Couldn't find item {itemName}" );
            return;
        }

        if ( !(Local?.Hotbar?.TryGiveItem( item ) ?? false) )
        {
            Log.Warning( $"Couldn't give item {itemName}" );
        }
    }
}