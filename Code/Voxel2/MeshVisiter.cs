using System;

namespace Voxel;

public class MeshVisiter : IDisposable
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

	public void Dispose()
	{
		Dispose( true );
		GC.SuppressFinalize( this );
	}

	protected virtual void Dispose( bool disposing )
	{
		if ( disposing )
		{
			visitXN = null;
			visitXP = null;
			visitZN = null;
			visitZP = null;
			visitYN = null;
			visitYP = null;
		}
	}

	~MeshVisiter()
	{
		Dispose( false );
	}
}
