using System;

namespace Voxel;

[GameResource( "Palette", "palette", "Palette" )]
public class Palette : GameResource
{
	/// <summary>
	/// Get the block index for the specified Block type.
	/// </summary>
	/// <param name="block"></param>
	/// <param name="fallbackIndex">Return this index if no block was found.</param>
	public byte GetBlockIndex( Block block, byte fallbackIndex = 1 )
	{
		return block is null ? fallbackIndex : CollectionExtensions.GetValueOrDefault( BlockToIndex, block.ResourceId, fallbackIndex );
	}

	private Dictionary<int, byte> BlockToIndex { get; set; } = new();
	public List<Block> Blocks { get; set; }

	public static Action OnReload { get; set; }

	private void RebuildCache()
	{
		BlockToIndex.Clear();

		for ( var index = 0; index < Blocks.Count; index++ )
		{
			var block = Blocks[index];
			BlockToIndex[block.ResourceId] = (byte)(index + 1);
		}
	}

	protected override void PostLoad()
	{
		RebuildCache();
		base.PostLoad();
	}

	protected override void PostReload()
	{
		base.PostReload();

		RebuildCache();
		OnReload?.Invoke();
	}
}
