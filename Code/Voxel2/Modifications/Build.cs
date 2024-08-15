namespace Voxel.Modifications;

#nullable enable

public record struct BuildModification( Block Block, Vector3Int Min, Vector3Int Max ) : IModification
{
	public ModificationKind Kind => ModificationKind.Build;

	public bool ShouldCreateChunk( Vector3Int chunkMin ) => true;

	public BuildModification( ByteStream stream, Vector3Int min, Vector3Int max )
		: this( ResourceLibrary.Get<Block>( stream.Read<int>() ), min, max )
	{

	}

	public void Write( ref ByteStream stream )
	{
		stream.Write( Block.ResourceId );
	}

	public void Apply( VoxelRenderer renderer, Chunk chunk )
	{
		var min = Min;
		var max = Max;

		var localMin = Vector3Int.Max( min - chunk.WorldMin, 0 );
		var localMax = Vector3Int.Min( max - chunk.WorldMin, Constants.ChunkSize - 1 );

		var index = renderer.Palette.GetBlockIndex( Block );

		for ( var x = localMin.x; x <= localMax.x; x++ )
		for ( var z = localMin.z; z <= localMax.z; z++ )
		for ( var y = localMin.y; y <= localMax.y; y++ )
		{
			chunk.SetVoxel( x, y, z, index );
		}
	}
}

