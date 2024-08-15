using System;

namespace Voxel;

[GameResource( "Palette", "palette", "Palette" )]
public partial class Palette : GameResource
{
	public struct Material
	{
		public Material()
		{
			Name = "Unnamed";
			Color = default;
			Texture = null;
		}

		[KeyProperty] public string Name { get; set; }
		public Color Color { get; set; }

		[Group( "Texture" )]
		public Texture Texture { get; set; }

		[Group( "Texture" )]
		public Vector2 TextureSize { get; set; } = 32;
	}

	public List<Material> Materials { get; set; }

	public static Action OnReload { get; set; }

	protected override void PostReload()
	{
		base.PostReload();

		OnReload?.Invoke();
	}
}
