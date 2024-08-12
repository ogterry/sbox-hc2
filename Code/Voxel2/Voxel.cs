using System.Runtime.InteropServices;

namespace Voxel;

[StructLayout( LayoutKind.Sequential, Pack = 1 )]
public partial struct Voxel
{
	public byte index;
}
