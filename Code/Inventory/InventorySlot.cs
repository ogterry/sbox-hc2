namespace HC2;

[Category( "Inventory" )]
[Description( "Can contain an item in an inventory." )]
[Icon( "check_box_outline_blank" )]
public class InventorySlot : Component
{
	/// <summary>
	/// Get the <see cref="Inventory"/> this slot belongs to.
	/// </summary>
	public Inventory Container => Components.GetInAncestorsOrSelf<Inventory>();

	/// <summary>
	/// Get the <see cref="Item"/> contained in this slot.
	/// </summary>
	public Item Item => Components.GetInChildren<Item>();
	
	/// <summary>
	/// Get the index of this slot in its parent inventory.
	/// </summary>
	public int SlotIndex { get; set; }
}
