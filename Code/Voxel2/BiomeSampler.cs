using System;
using Sandbox.Utility;

namespace Voxel;

public partial class BiomeSampler : Component
{
	[Property] public Biome[] Biomes { get; set; }
	[Property] public float BiomeSize { get; set; } = 4f;

	public Biome GetBiomeAt( int x, int y )
	{
		var temperature = Noise.Simplex( (x / BiomeSize), (y / BiomeSize) );

		Biome selectedBiome = null;
		var minDeviation = float.MaxValue;
		
		for ( var i = 0; i < Biomes.Length; i++ )
		{
			var biome = Biomes[i];
			var deviation = MathF.Abs( biome.Temperature - temperature );
			
			if ( deviation >= minDeviation )
				continue;
			
			minDeviation = deviation;
			selectedBiome = biome;
		}
		
		return selectedBiome;
	}
}
