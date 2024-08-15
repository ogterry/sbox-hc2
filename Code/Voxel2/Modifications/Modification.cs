namespace Voxel.Modifications;

#nullable enable

public enum ModificationKind : byte
{
	// TODO: if we move this to a library, need to make this dynamic

	WorldGen,
	Carve
}

public interface IModification
{
	ModificationKind Kind { get; }

	Vector3Int Min { get; }
	Vector3Int Max { get; }

	bool ShouldCreateChunk( Vector3Int chunkMin );

	void Write( ref ByteStream stream );
	void Apply( VoxelRenderer renderer, Chunk chunk );
}
