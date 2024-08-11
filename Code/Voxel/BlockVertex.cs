using System.Runtime.InteropServices;

namespace HC2;

[StructLayout( LayoutKind.Sequential )]
public readonly struct BlockVertex
{
	private readonly uint data;

	public BlockVertex( uint x, uint y, uint z, uint faceData )
	{
		data = faceData | (x & 63) | (y & 63) << 6 | (z & 63) << 12;
	}

	public static readonly VertexAttribute[] Layout =
	{
		new( VertexAttributeType.TexCoord, VertexAttributeFormat.UInt32, 1, 10 )
	};
}
