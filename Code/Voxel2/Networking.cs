
using System;

namespace Voxel;

#nullable enable

public enum ModificationKind : byte
{
	// TODO: if we move this to a library, need to make this dynamic

	WorldGen,
	Explode
}

public interface IModification
{
	ModificationKind Kind { get; }

	Vector3Int Min { get; }
	Vector3Int Max { get; }

	void Write( ref ByteStream stream );
	void Apply( Chunk chunk );
}

[Icon( "cell_tower" )]
public sealed class VoxelNetworking : Component
{
	// TODO: Spatial partitioning? only send nearby modifications?
	// TODO: Erase redundant modifications from history?

	[RequireComponent] public VoxelRenderer Renderer { get; private set; } = null!;

	private record struct Modification( int Index, ModificationKind Kind, Vector3Int Min, Vector3Int Max, byte[] Data );

	private readonly List<Modification> _history = new();
	private int _nextIndex;

	public void Modify<T>( T modification )
		where T : struct, IModification
	{
		if ( IsProxy ) throw new Exception( "Can't modify proxies." );

		var writer = ByteStream.Create( 64 );

		try
		{
			modification.Write( ref writer );

			var serialized = new Modification( _nextIndex++,
				modification.Kind, modification.Min, modification.Max,
				writer.ToArray() );

			_history.Add( serialized );
		}
		finally
		{
			writer.Dispose();
		}
	}

	public void Apply<T>( T modification )
		where T : struct, IModification
	{

	}
}
