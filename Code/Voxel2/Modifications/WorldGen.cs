using System;
using Sandbox.Utility;

namespace Voxel.Modifications;

public record struct WorldGenModification( int Seed, int ResourceId, Vector3Int Min, Vector3Int Max ) : IModification
{
	public ModificationKind Kind => ModificationKind.WorldGen;
	public bool CreateChunks => true;

	public WorldGenModification( ByteStream stream )
		: this( stream.Read<int>(), stream.Read<int>(), stream.Read<Vector3Int>(), stream.Read<Vector3Int>() )
	{

	}

	public void Write( ref ByteStream stream )
	{
		stream.Write( Seed );
		stream.Write( ResourceId );
		stream.Write( Min );
		stream.Write( Max );
	}

	private static Transform GetRandomNoiseTransform( Random random, float scale )
	{
		var pos = new Vector3(
			random.Float( -32_768f, 32_768f ),
			random.Float( -32_768f, 32_768f ),
			random.Float( -32_768f, 32_768f ) );

		var rotation = random.Rotation();

		return new Transform( pos, rotation, scale );
	}

	public void Apply( Scene scene, Chunk chunk )
	{
		var min = chunk.WorldMin;
		var random = new Random( Seed );
		var resource = ResourceLibrary.Get<WorldGenParameters>( ResourceId );

		var biomeNoiseTransform = GetRandomNoiseTransform( random, 0.1f );
		var surfaceNoiseTransform = GetRandomNoiseTransform( random, 0.4f );
		var biomeSampler = scene.GetAllComponents<BiomeSampler>().FirstOrDefault();

		var center = (Min + Max) / 2;
		var radius = (Max - Min).x / 2;

		chunk.Clear();

		var voxels = chunk.Voxels;

		var plainsGroundLevel = resource.PlainsHeight;
		var mountainsGroundLevel = resource.MountainsHeight;

		for ( var x = 0; x < Constants.ChunkSize; x++ )
		{
			for ( var z = 0; z < Constants.ChunkSize; z++ )
			{
				var worldX = min.x + x;
				var worldZ = min.z + z;

				var centerDistSq = (worldX - center.x) * (worldX - center.x) + (worldZ - center.z) * (worldZ - center.z);
				var centrality = Math.Clamp( 4f - 4f * MathF.Sqrt( centerDistSq ) / radius, 0f, 1f );

				var surfaceNoisePos = surfaceNoiseTransform.PointToWorld( new Vector3( worldX, 0f, worldZ ) );
				var surfaceNoise = Math.Clamp( Noise.Fbm( 4, surfaceNoisePos.x, surfaceNoisePos.y, surfaceNoisePos.z ), 0f, 1f ) * centrality;
				var biomeNoisePos = biomeNoiseTransform.PointToWorld( new Vector3( worldX, 0f, worldZ ) );
				var biomeNoise = Math.Clamp( Noise.Fbm( 3, biomeNoisePos.x, biomeNoisePos.y, biomeNoisePos.z ) * 2f - 0.5f, 0f, 1f );

				biomeNoise = MathF.Pow( biomeNoise, 4f ) * centrality;

				var biome = biomeSampler?.GetBiomeAt( worldX, worldZ );

				var plainsHeight = plainsGroundLevel.Evaluate( surfaceNoise );
				var mountainsHeight = mountainsGroundLevel.Evaluate( surfaceNoise );

				var height = (int) (plainsHeight + (mountainsHeight - plainsHeight) * biomeNoise) - min.y;

				var i = Chunk.WorldToLocal( x );
				var k = Chunk.WorldToLocal( z );

				var minJ = int.MaxValue;
				var maxJ = int.MinValue;

				var soilDepth = (int) (8f - biomeNoise * 16f);

				for ( var y = 0; y < Constants.ChunkSize && y < height; y++ )
				{
					var worldY = min.y + y;

					byte blockType;

					if ( y < height - soilDepth )
					{
						blockType = biome?.UnderSurfaceId ?? 2;
					}
					else if ( soilDepth > 2 && y < height - 1 )
					{
						blockType = biome?.SurfaceBlockId ?? 1;
					}
					else
					{
						blockType = biome?.DeepBlockId ?? 3;
					}

					var j = Chunk.WorldToLocal( y );

					voxels[Chunk.GetAccessLocal( i, j, k )] = blockType;

					minJ = Math.Min( j, minJ );
					maxJ = Math.Max( j, maxJ );
				}

				if ( maxJ < 0 || minJ > 255 ) continue;

				var heightmapAccess = Chunk.GetHeightmapAccess( i, k );

				chunk.MinAltitude[heightmapAccess] = (byte)minJ;
				chunk.MaxAltitude[heightmapAccess] = (byte)maxJ;
			}
		}
	}
}

[Icon( "casino" )]
public sealed class VoxelWorldGen : Component, Component.ExecuteInEditor
{
	[RequireComponent] public VoxelNetworking VoxelNetworking { get; private set; }

	[Property]
	public int Seed { get; set; }

	[Property]
	public WorldGenParameters Parameters { get; set; }

	[Button( "Randomize" )]
	private void Randomize()
	{
		Seed = Random.Shared.Next();
		Regenerate();
	}

	[Button( "Regenerate" )]
	private void Regenerate()
	{
		VoxelNetworking.Modify( new WorldGenModification( Seed, Parameters.ResourceId, 0, VoxelNetworking.Renderer.Size ) );
	}

	protected override void OnEnabled()
	{
		Regenerate();
	}
}

[GameResource( "World Gen Parameters", "worldgen", "Parameters for the voxel world generator.", Icon = "public" )]
public sealed class WorldGenParameters : GameResource
{
	public Curve PlainsHeight { get; set; }
	public Curve MountainsHeight { get; set; }
}
