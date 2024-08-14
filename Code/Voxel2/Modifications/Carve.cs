namespace Voxel.Modifications;

#nullable enable

public record struct CarveModification( Vector3Int Origin, byte Radius, int Seed ) : IModification
{
	public ModificationKind Kind => ModificationKind.Carve;
	public Vector3Int Min => Origin - Radius;
	public Vector3Int Max => Origin + Radius;
	public bool CreateChunks => false;

	public CarveModification( ByteStream stream )
		: this( stream.Read<Vector3Int>(), stream.Read<byte>(), stream.Read<int>() )
	{

	}

	public void Write( ref ByteStream stream )
	{
		stream.Write( Origin );
		stream.Write( Radius );
		stream.Write( Seed );
	}

	public void Apply( Scene scene, Chunk chunk )
	{
		// TODO: rough sphere

		var min = Min;
		var max = Max;

		var localMin = Vector3Int.Max( min - chunk.WorldMin, 0 );
		var localMax = Vector3Int.Min( max - chunk.WorldMin, Constants.ChunkSize );

		for ( var x = localMin.x; x <= localMax.x; x++ )
		for ( var y = localMin.y; y <= localMax.y; y++ )
		for ( var z = localMin.z; z <= localMax.z; z++ )
		{
			chunk.SetVoxel( x, y, z, 0 );
		}
	}
}
