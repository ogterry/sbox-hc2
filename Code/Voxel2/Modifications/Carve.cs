using HC2;

namespace Voxel.Modifications;

#nullable enable

public record struct CarveModification( GatherSourceKind SourceKind, byte Damage, Vector3Int Origin, byte Radius, int Seed ) : IModification
{
	public ModificationKind Kind => ModificationKind.Carve;
	public Vector3Int Min => Origin - Radius;
	public Vector3Int Max => Origin + Radius;

	public CarveModification( ByteStream stream )
		: this( stream.Read<GatherSourceKind>(), stream.Read<byte>(), stream.Read<Vector3Int>(), stream.Read<byte>(), stream.Read<int>() )
	{

	}

	public bool ShouldCreateChunk( Vector3Int chunkMin ) => false;

	public void Write( ref ByteStream stream )
	{
		stream.Write( SourceKind );
		stream.Write( Damage );
		stream.Write( Origin );
		stream.Write( Radius );
		stream.Write( Seed );
	}

	public void Apply( VoxelRenderer renderer, Chunk chunk )
	{
		// TODO: rough sphere

		var min = Min;
		var max = Max;

		var localMin = Vector3Int.Max( min - chunk.WorldMin, 0 );
		var localMax = Vector3Int.Min( max - chunk.WorldMin, Constants.ChunkSize );

		var voxels = chunk.Voxels;
		var palette = renderer.Palette;

		for ( var x = localMin.x; x <= localMax.x; x++ )
		for ( var y = localMin.y; y <= localMax.y; y++ )
		for ( var z = localMin.z; z <= localMax.z; z++ )
		{
			var index = voxels[Chunk.GetAccessLocal( x, y, z )];
			if ( index == 0 ) continue;

			var entry = palette.GetEntry( index );
			if ( entry.IsEmpty || entry.Health == 0 ) continue;

			if ( entry.Health == 1 )
			{
				chunk.SetVoxel( x, y, z, 0 );
			}
			else
			{
				chunk.SetVoxel( x, y, z, (byte) (index + 1) );
			}
		}
	}
}
