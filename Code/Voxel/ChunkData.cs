
namespace HC2;

public partial class ChunkData
{
	public Vector3Int Offset;
	public byte[] BlockTypes;

	public ChunkData()
	{
	}

	public ChunkData( Vector3Int offset )
	{
		Offset = offset;
		BlockTypes = new byte[Chunk.ChunkSize * Chunk.ChunkSize * Chunk.ChunkSize];
	}

	public byte GetBlockTypeAtPosition( Vector3Int pos )
	{
		return BlockTypes[Chunk.GetBlockIndexAtPosition( pos )];
	}

	public byte GetBlockTypeAtIndex( int index )
	{
		return BlockTypes[index];
	}

	public void SetBlockTypeAtPosition( Vector3Int pos, byte blockType )
	{
		BlockTypes[Chunk.GetBlockIndexAtPosition( pos )] = blockType;
	}

	public void SetBlockTypeAtIndex( int index, byte blockType )
	{
		BlockTypes[index] = blockType;
	}
}
