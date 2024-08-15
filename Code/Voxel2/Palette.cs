using System;
using System.Text.Json.Serialization;

namespace Voxel;

public record struct PaletteEntry( Block Block, int Health )
{
	public bool IsEmpty => Block is null;
}

[GameResource( "Palette", "palette", "Palette" )]
public class Palette : GameResource
{
	/// <summary>
	/// Get the block index for the specified Block type.
	/// </summary>
	public byte GetBlockIndex( Block block )
	{
		return GetBlockIndex( block, block?.MaxHealth ?? 0);
	}

	public byte GetBlockIndex( Block block, int health )
	{
		if ( block is null ) return 0;

		health = block.IsBreakable ? Math.Clamp( health, 1, block.MaxHealth ) : 0;
		return EntryLookup.GetValueOrDefault( (block.ResourceId, health) );
	}

	public PaletteEntry GetEntry( byte index )
	{
		return Entries[index];
	}

	[Hide, JsonIgnore]
	private Dictionary<(int BlockId, int Health), byte> EntryLookup { get; } = new();

	[Hide, JsonIgnore]
	private PaletteEntry[] Entries { get; } = new PaletteEntry[256];

	[JsonInclude]
	public List<Block> Blocks { get; set; }

	public static Action OnReload { get; set; }

	private void RebuildCache()
	{
		EntryLookup.Clear();

		Array.Clear( Entries );

		var nextIndex = 1;

		foreach ( var block in Blocks )
		{
			var breakable = block.IsBreakable;
			var health = breakable ? Math.Max( block.MaxHealth, 1 ) : 1;

			if ( nextIndex + health > 256 )
			{
				Log.Warning( "Palette is overflowing! Too many blocks / too much health per block." );
				break;
			}

			if ( breakable )
			{
				for ( var i = 0; i < health; ++i )
				{
					AddEntry( block, health - i, ref nextIndex );
				}
			}
			else
			{
				AddEntry( block, 0, ref nextIndex );
			}
		}
	}

	private void AddEntry( Block block, int health, ref int nextIndex )
	{
		var entry = new PaletteEntry( block, health );

		EntryLookup[(block.ResourceId, health)] = (byte)nextIndex;
		Entries[nextIndex++] = entry;
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
