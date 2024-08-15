namespace Voxel.Modifications;

#nullable enable

public enum ModificationKind : byte
{
	// TODO: if we move this to a library, need to make this dynamic

	WorldGen,
	Carve,
	Build
}

/// <summary>
/// Interface for networked modifications. To create a new one:
///
/// <list type="number">
/// <item>Add an entry to <see cref="ModificationKind"/></item
/// <item>Return that in your <see cref="Kind"/> implementation</item>
/// <item>Add a case in <see cref="VoxelNetworking.Apply(VoxelNetworking.Modification)"/></item>
/// </list>
/// </summary>
public interface IModification
{
	ModificationKind Kind { get; }

	Vector3Int Min { get; }
	Vector3Int Max { get; }

	/// <summary>
	/// If a chunk that's within Min and Max doesn't exist, should we create it?
	/// </summary>
	bool ShouldCreateChunk( Vector3Int chunkMin );

	void Write( ref ByteStream stream );
	void Apply( VoxelRenderer renderer, Chunk chunk );
}
