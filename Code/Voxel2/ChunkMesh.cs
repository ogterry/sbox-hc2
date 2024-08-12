using Sandbox.Diagnostics;

namespace Voxel;

public partial class ChunkMesh
{
	public Map m;

	// Store a reference to the linked chunk and its neighbours
	public Chunk chunk;
	Chunk chunkXN, chunkXP, chunkYN, chunkYP, chunkZN, chunkZP;

	public MeshVisiter meshVisiter;

	// Shortcuts
	int ChunkPosX => chunk.chunkPosX;
	int ChunkPosY => chunk.chunkPosY;
	int ChunkPosZ => chunk.chunkPosZ;

	public Vector3 WorldPos;

	public ChunkMesh( Map m, Chunk c )
	{
		Assert.True( c != null );
		Assert.True( c.Allocated );

		this.m = m;
		chunk = c;

		WorldPos = new( ChunkPosX * Constants.ChunkSize, ChunkPosY * Constants.ChunkSize, ChunkPosZ * Constants.ChunkSize );

		GetNeighbourReferences();
	}

	public void GetNeighbourReferences()
	{
		var chunkAccess = m.GetAccessLocal( ChunkPosX, ChunkPosY, ChunkPosZ );
		var exteriorChunk = m.ShouldMeshExterior ? Map.EMPTY_CHUNK : Map.FULL_CHUNK;

		chunkXN = ChunkPosX > 0 ? m.chunks[chunkAccess - CHUNK_STEP_X] : exteriorChunk;
		chunkXP = ChunkPosX < m.ChunkAmountXM1 ? m.chunks[chunkAccess + CHUNK_STEP_X] : exteriorChunk;

		chunkYN = ChunkPosY > 0 ? m.chunks[chunkAccess - CHUNK_STEP_Y] : exteriorChunk;
		chunkYP = ChunkPosY < m.ChunkAmountYM1 ? m.chunks[chunkAccess + CHUNK_STEP_Y] : exteriorChunk;

		chunkZN = ChunkPosZ > 0 ? m.chunks[chunkAccess - CHUNK_STEP_Z] : exteriorChunk;
		chunkZP = ChunkPosZ < m.ChunkAmountZM1 ? m.chunks[chunkAccess + CHUNK_STEP_Z] : exteriorChunk;
	}

	const int CHUNK_STEP_Y = 1;
	const int CHUNK_STEP_X = Constants.MaxChunkAmount;
	const int CHUNK_STEP_Z = Constants.MaxChunkAmountSquared;
}
