namespace Voxel;

public static class Constants
{
	public const int ChunkSize = 32;
	public const int ChunkSizeSquared = ChunkSize * ChunkSize;
	public const int ChunkSizeCubed = ChunkSize * ChunkSize * ChunkSize;
	public const int ChunkSizeM1 = ChunkSize - 1;


	public const int MaxChunkAmount = 32;
	public const int MaxChunkAmountSquared = MaxChunkAmount * MaxChunkAmount;
	public const int MaxChunkAmountCubed = MaxChunkAmount * MaxChunkAmount * MaxChunkAmount;

	public const int ChunkMask = 0x1f;
	public const int ChunkShift = 5;

	public static float VoxelSize => 16.0f;
}
