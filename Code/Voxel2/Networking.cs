using System;
using Voxel.Modifications;

namespace Voxel;

[Icon( "cell_tower" )]
public sealed class VoxelNetworking : Component, Component.ExecuteInEditor
{
	// TODO: Send history to late joiners
	// TODO: Spatial partitioning? only send nearby modifications?
	// TODO: Erase redundant modifications from history?

	[RequireComponent] public VoxelRenderer Renderer { get; private set; } = null!;

	private record struct Modification( int Index, ModificationKind Kind, Vector3Int Min, Vector3Int Max, byte[] Data );

	private readonly List<Modification> _history = new();
	private readonly Queue<Modification> _toApply = new();

	private int _nextIndex;

	public void Modify<T>( T modification )
		where T : struct, IModification
	{
		var writer = ByteStream.Create( 64 );

		try
		{
			modification.Write( ref writer );

			var index = IsProxy ? -1 : _nextIndex++;
			var serialized = new Modification( index,
				modification.Kind, modification.Min, modification.Max,
				writer.ToArray() );

			if ( !IsProxy )
			{
				_history.Add( serialized );
				BroadcastSingle( serialized );
			}
			else
			{
				_toApply.Enqueue( serialized );
			}
		}
		finally
		{
			writer.Dispose();
		}

		if ( _toApply.Count == 1 && Renderer.IsReady )
		{
			ApplyPending();
		}
	}

	private void ApplyPending()
	{
		if ( _toApply.Count == 0 ) return;

		while ( _toApply.TryDequeue( out var modification ) )
		{
			Apply( modification );
		}

		Renderer.MeshChunks();
	}

	private void Apply( Modification modification )
	{
		// TODO: reflection

		using var stream = ByteStream.CreateReader( modification.Data );

		switch ( modification.Kind )
		{
			case ModificationKind.WorldGen:
				Apply( new WorldGenModification( stream ) );
				break;

			case ModificationKind.Carve:
				Apply( new CarveModification( stream ) );
				break;

			default:
				throw new NotImplementedException();
		}
	}

	private void Apply<T>( T modification )
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

			modification.Apply( Scene, chunk );
		}

		model.SetRegionDirty( modification.Min, modification.Max );
	}

	protected override void OnUpdate()
	{
		if ( Renderer.IsReady )
		{
			ApplyPending();
		}
	}

	private void BroadcastSingle( Modification modification )
	{
		BroadcastSingle( modification.Index, modification.Kind, modification.Min, modification.Max, modification.Data );
	}

	[Broadcast( NetPermission.HostOnly )]
	private void BroadcastSingle( int index, ModificationKind kind, Vector3Int min, Vector3Int max, byte[] data )
	{
		_toApply.Enqueue( new Modification( index, kind, min, max, data ) );
	}
}
