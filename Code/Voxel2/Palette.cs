
namespace Voxel;

[GameResource( "Palette", "palette", "Palette" )]
public partial class Palette : GameResource
{
	public struct Material
	{
		public Color Color { get; set; }
		public Texture Texture { get; set; }
	}

	public List<Material> Materials { get; set; }
}
