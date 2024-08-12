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
		visitXN = new int[Constants.ChunkSizeCubed];
		visitXP = new int[Constants.ChunkSizeCubed];
		visitZN = new int[Constants.ChunkSizeCubed];
		visitZP = new int[Constants.ChunkSizeCubed];
		visitYN = new int[Constants.ChunkSizeCubed];
		visitYP = new int[Constants.ChunkSizeCubed];
	}

	public void Reset()
	{
		Array.Clear( visitXN, 0, Constants.ChunkSizeCubed );
		Array.Clear( visitXP, 0, Constants.ChunkSizeCubed );
		Array.Clear( visitZN, 0, Constants.ChunkSizeCubed );
		Array.Clear( visitZP, 0, Constants.ChunkSizeCubed );
		Array.Clear( visitYN, 0, Constants.ChunkSizeCubed );
		Array.Clear( visitYP, 0, Constants.ChunkSizeCubed );
	}
}
