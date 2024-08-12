namespace Voxel;

public partial class Chunk
{
	public Voxel[] voxels;
	public int bytes_voxels;

	public bool Allocated => voxels != null && minAltitude != null && maxAltitude != null;

	public bool dirty;
	public bool fake;

	public byte chunkPosX;
	public byte chunkPosY;
	public byte chunkPosZ;

	public byte[] maxAltitude;
	public byte[] minAltitude;
}
