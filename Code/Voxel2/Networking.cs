
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

	bool CreateChunks { get; }

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

		Apply( modification );
	}

	public void Apply<T>( T modification )
		where T : struct, IModification
	{
		var model = Renderer.Model;

		var chunkMin = model.ClampChunkCoords( model.GetChunkCoords( modification.Min ) );
		var chunkMax = model.ClampChunkCoords( model.GetChunkCoords( modification.Max - 1 ) );

		var createChunks = modification.CreateChunks;

		for ( var f = chunkMin.x; f <= chunkMax.x; ++f )
		for ( var g = chunkMin.y; g <= chunkMax.y; ++g )
		for ( var h = chunkMin.z; h <= chunkMax.z; ++h )
		{
			if ( model.InitChunkLocal( f, g, h, createChunks ) is not { } chunk )
			{
				continue;
			}

			modification.Apply( chunk );
		}

		model.SetRegionDirty( modification.Min, modification.Max );
	}
}
