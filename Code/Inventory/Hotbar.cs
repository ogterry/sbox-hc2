using System;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace HC2;

public record ItemEquipEvent( Item Item ) : IGameEvent;
public record ItemUnequipEvent( Item Item ) : IGameEvent;

[Category( "Inventory - Hotbar" )]
[Description( "The player's hotbar, with an active slot." )]
[Icon( "inventory_2" )]
public class Hotbar : Inventory
{
    public int SelectedSlot { get; private set; } = 0;
    public Item SelectedItem => GetItemInSlot( SelectedSlot );

    protected override void OnUpdate()
    {
        base.OnUpdate();

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

    public void SelectSlot( int slot )
    {
        if ( slot < 0 || slot >= MaxSlots )
        {
            throw new ArgumentOutOfRangeException( nameof( slot ) );
        }
        if ( slot == SelectedSlot )
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
        }
    }

}
