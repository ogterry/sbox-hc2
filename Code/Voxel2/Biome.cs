using System;

namespace Voxel;

[GameResource( "Biome", "biome", "Biome", Icon = "local_florist" )]
public partial class Biome : GameResource
{
	/// <summary>
	/// The name of this biome.
	/// </summary>
	public string Name { get; set; }
	
	/// <summary>
	/// The temperature of this biome (0 is very cold, 1 is very hot). This will be used to
	/// determine which biome is selected for an area during world generation.
	/// </summary>
	[Range( 0f, 1f )] public float Temperature { get; set; } = 0.5f;

	/// <summary>
	/// The block type to use for the surface.
	/// </summary>
	public Block SurfaceBlock { get; set; }
	
	/// <summary>
	/// The block type to use for the blocks just below the surface.
	/// </summary>
	public Block UnderSurfaceBlock { get; set; }
	
	/// <summary>
	/// The block type to use for blocks deep below the surface.
	/// </summary>
	public Block DeepBlock { get; set; }
}
