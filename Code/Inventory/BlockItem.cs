using System.IO;
using Voxel;

namespace HC2;

public class BlockItem : Item
{
	/// <summary>
	/// Get the block type of this item.
	/// </summary>
	public Block BlockType { get; init; }
	
	public override string Name => BlockType?.Name ?? base.Name;
	public override int MaxStack => BlockType?.MaxStack ?? base.MaxStack;
	
	/// <summary>
	/// Create a new block item from the specified Block type.
	/// </summary>
	/// <param name="blockType"></param>
	/// <param name="amount"></param>
	public static BlockItem Create( Block blockType, int amount = 1 )
	{
		// Conna: this is a hack. Try to find an ItemAsset type with the resource name block.
		// We should instead define a reference to this block type somewhere and look it up there.
		var resource = ResourceLibrary.GetAll<ItemAsset>()
			.FirstOrDefault( x => x.ResourceName == "block" );

		if ( resource is null )
			throw new FileNotFoundException( "Unable to find a block.item ItemAsset file!" );
		
		return new() { BlockType = blockType, Resource = resource, Amount = amount };
	}
}
