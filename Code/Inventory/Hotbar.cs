using System;
using Sandbox.Diagnostics;
using Sandbox.Events;
using Voxel;

namespace HC2;

public record ItemEquipEvent( Item Item ) : IGameEvent;
public record ItemUnequipEvent( Item Item ) : IGameEvent;
public record ItemUseEvent( float DurabilityUsed ) : IGameEvent;


[Category( "Inventory - Hotbar" )]
[Description( "The player's hotbar, with an active slot." )]
[Icon( "inventory_2" )]
public class Hotbar : Inventory
{
    public int SelectedSlot { get; private set; } = 0;
    public Item SelectedItem => GetItemInSlot( SelectedSlot );

    Item LastEquippedItem;

    protected override void OnUpdate()
    {
        base.OnUpdate();

        if ( SelectedItem != LastEquippedItem )
        {
            SelectSlot( SelectedSlot, true );
        }

        for ( int i = 1; i <= MaxSlots; i++ )
        {
            if ( Input.Pressed( "Slot" + i ) )
            {
                SelectSlot( i - 1 );
            }
        }

        if ( Input.MouseWheel.y < 0 )
        {
            SelectSlot( (SelectedSlot + 1) % MaxSlots );
        }
        else if ( Input.MouseWheel.y > 0 )
        {
            SelectSlot( (SelectedSlot - 1 + MaxSlots) % MaxSlots );
        }
    }

    public void SelectSlot( int slot, bool force = false )
    {
        if ( slot < 0 || slot >= MaxSlots )
        {
            throw new ArgumentOutOfRangeException( nameof( slot ) );
        }
        if ( slot == SelectedSlot && !force )
        {
            return;
        }

        var selectedItem = SelectedItem;
        if ( selectedItem is not null )
        {
            GameObject.Dispatch( new ItemUnequipEvent( selectedItem ) );
        }

        SelectedSlot = slot;
        
        var newItem = SelectedItem;
        if ( newItem is not null )
        {
            GameObject.Dispatch( new ItemEquipEvent( newItem ) );
            LastEquippedItem = newItem;
        }
        else
        {
            if ( LastEquippedItem is not null )
            {
                GameObject.Dispatch( new ItemUnequipEvent( LastEquippedItem ) );
            }
            
            LastEquippedItem = null;
        }
    }
}
