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

	/// <summary>
	/// How thick is the <see cref="SurfaceBlock"/> layer?
	/// </summary>
	public int SurfaceDepth { get; set; } = 1;

	/// <summary>
	/// How thick is the <see cref="UnderSurfaceBlock"/> layer?
	/// </summary>
	public int UnderSurfaceDepth { get; set; } = 4;

	/// <summary>
	/// Block at a depth under the surface.
	/// </summary>
	/// <param name="surfaceHeight">Surface height at this voxel column.</param>
	/// <param name="terrain">Terrain noise value from world gen, 0 is plains, 1 is mountain.</param>
	/// <param name="depth">Depth from surface, 0 for the top-most block.</param>
	public Block GetBlock( int surfaceHeight, float terrain, int depth )
	{
		if ( terrain > 0.2f ) return DeepBlock;
		if ( depth < SurfaceDepth && terrain < 0.1f ) return SurfaceBlock;
		if ( depth - SurfaceDepth < UnderSurfaceDepth ) return UnderSurfaceBlock;
		return DeepBlock;
	}
}
