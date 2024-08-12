namespace Voxel;

public struct VoxelVertex
{
	public static readonly VertexAttribute[] Layout =
	{
		new( VertexAttributeType.TexCoord, VertexAttributeFormat.UInt32, 1, 10 )
	};
}
