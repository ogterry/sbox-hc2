using System;

namespace Voxel;

public class MeshVisiter
{
	public int Comparison = 1;
	public int[] visitXN;
	public int[] visitXP;
	public int[] visitZN;
	public int[] visitZP;
	public int[] visitYN;
	public int[] visitYP;

	public MeshVisiter()
	{
		visitXN = new int[Constants.VOXELS_PER_CHUNK];
		visitXP = new int[Constants.VOXELS_PER_CHUNK];
		visitZN = new int[Constants.VOXELS_PER_CHUNK];
		visitZP = new int[Constants.VOXELS_PER_CHUNK];
		visitYN = new int[Constants.VOXELS_PER_CHUNK];
		visitYP = new int[Constants.VOXELS_PER_CHUNK];
	}

	public void Reset()
	{
		Array.Clear( visitXN, 0, Constants.VOXELS_PER_CHUNK );
		Array.Clear( visitXP, 0, Constants.VOXELS_PER_CHUNK );
		Array.Clear( visitZN, 0, Constants.VOXELS_PER_CHUNK );
		Array.Clear( visitZP, 0, Constants.VOXELS_PER_CHUNK );
		Array.Clear( visitYN, 0, Constants.VOXELS_PER_CHUNK );
		Array.Clear( visitYP, 0, Constants.VOXELS_PER_CHUNK );
	}
}
