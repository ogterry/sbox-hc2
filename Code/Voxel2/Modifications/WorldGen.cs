using System;
using HC2;
using Sandbox.Diagnostics;
using Sandbox.Utility;

namespace Voxel.Modifications;

#nullable enable

[Icon( "casino" )]
public sealed partial class VoxelWorldGen : Component, Component.ExecuteInEditor
{
	[RequireComponent] public VoxelNetworking VoxelNetworking { get; private set; } = null!;
	[RequireComponent] public VoxelRenderer Renderer { get; private set; } = null!;

	[Property]
	public int Seed { get; set; }

	[Property]
	public WorldGenParameters? Parameters { get; set; }

	private readonly List<GameObject> _spawnedObjects = new();

	[Button( "Randomize" )]
	public void Randomize()
	{
		Seed = Random.Shared.Next();
		Regenerate();
	}

	[Button( "Regenerate" )]
	private void Regenerate()
	{
		if ( Parameters is null ) return;

		using var sceneScope = Scene.Push();

		var heightmap = Generate();

		if ( WorldPersistence.FileToLoad is null )
		{
			Log.Info( $"Applying heightmap! size: {heightmap.Size}" );

			Assert.AreEqual( VoxelNetworking.Renderer.Size.x, heightmap.Size );
			Assert.AreEqual( VoxelNetworking.Renderer.Size.z, heightmap.Size );

			VoxelNetworking.Modify( new HeightmapModification( 0, VoxelNetworking.Renderer.Size, heightmap.Samples ) );
		}
	}

	protected override void OnEnabled()
	{
		// While in the editor, and not playing, generate a map
		// Play mode is handled differently
		if ( !Game.IsPlaying )
		{
			Regenerate();
		}
	}

	[Button( "Destroy Props" )]
	private void DestroyProps()
	{
		foreach ( var spawnedObject in _spawnedObjects )
		{
			spawnedObject.Destroy();
		}

		_spawnedObjects.Clear();
	}

	private readonly record struct Circle( Vector2 Center, float Radius );

	/// <summary>
	/// Generates a heightmap, spawns props, returns heightmap.
	/// Doesn't actually apply the heightmap to the world.
	/// </summary>
	internal Heightmap Generate()
	{
		DestroyProps();

		Random = new Random( Seed );

		var size = VoxelNetworking.Renderer.Size.x;
		var heightmap = Heightmap = new Heightmap( size, Transform.World.WithScale( Constants.VoxelSize ) );

		var sampler = new WorldGenSampler( Parameters!, Seed, size );
		sampler.Sample( 0, 0, size, size, Heightmap.Samples );

		SpawnFeatures( Vector3.Zero, 4096f, true, Parameters!.Features.ToArray() );

		Random = null!;
		Heightmap = null!;

		return heightmap;
	}

	//
	// Helper properties for feature spawning graphs to use
	//

	public Random Random { get; private set; } = null!;
	public Heightmap Heightmap { get; private set; } = null!;
}

[GameResource( "World Gen Parameters", "worldgen", "Parameters for the voxel world generator.", Icon = "public" )]
public sealed class WorldGenParameters : GameResource
{
	public Curve TerrainBias { get; set; }
	public Curve PlainsHeight { get; set; }
	public Curve MountainsHeight { get; set; }

	// TODO: move these to biomes
	public List<WorldGenFeature> Features { get; set; } = new();
}

public record struct HeightmapSample( int Height, float Terrain );

public sealed class WorldGenSampler
{
	private static Transform GetRandomNoiseTransform( Random random, float scale )
	{
		var pos = new Vector3(
			random.Float( -32_768f, 32_768f ),
			random.Float( -32_768f, 32_768f ),
			random.Float( -32_768f, 32_768f ) );

		var rotation = random.Rotation();

		return new Transform( pos, rotation, scale );
	}

	private readonly int _worldSize;

	private readonly Transform _biomeNoiseTransform;
	private readonly Transform _heightNoiseTransform;

	private readonly Curve _terrainBias;
	private readonly Curve _plainsHeight;
	private readonly Curve _mountainsHeight;

	public WorldGenSampler( WorldGenParameters parameters, int seed, int worldSize )
	{
		var random = new Random( seed );

		_worldSize = worldSize;

		_biomeNoiseTransform = GetRandomNoiseTransform( random, 0.3f );
		_heightNoiseTransform = GetRandomNoiseTransform( random, 0.4f );

		_terrainBias = parameters.TerrainBias;
		_plainsHeight = parameters.PlainsHeight;
		_mountainsHeight = parameters.MountainsHeight;
	}

	public HeightmapSample Sample( int x, int z )
	{
		var edgeDist = Math.Min( Math.Min( x, z ), Math.Min( _worldSize - x, _worldSize - z ) ) / (_worldSize * 0.125f);
		var centrality = Math.Clamp( edgeDist, 0f, 1f );

		var heightNoisePos = _heightNoiseTransform.PointToWorld( new Vector3( x, z, 0f ) );
		var heightNoise = Math.Clamp( Noise.Fbm( 6, heightNoisePos.x, heightNoisePos.y, heightNoisePos.z ), 0f, 1f ) * centrality;

		var terrainNoisePos = _biomeNoiseTransform.PointToWorld( new Vector3( x, z, 0f ) );
		var terrainNoise = Math.Clamp( Noise.Fbm( 4, terrainNoisePos.x, terrainNoisePos.y, terrainNoisePos.z ) * centrality, 0f, 1f );

		terrainNoise = _terrainBias.Evaluate( terrainNoise );

		var plainsHeight = _plainsHeight.Evaluate( heightNoise );
		var mountainsHeight = _mountainsHeight.Evaluate( heightNoise );

		var height = (int)(plainsHeight + (mountainsHeight - plainsHeight) * terrainNoise);

		return new( height, terrainNoise );
	}

	public void Sample( int minX, int minZ, int sizeX, int sizeZ, Span<HeightmapSample> output )
	{
		Assert.True( output.Length >= sizeX * sizeZ );

		// TODO: batch noise calls nicer, so it doesn't need to reinit each call

		for ( var x = 0; x < sizeX; ++x )
			for ( var z = 0; z < sizeZ; ++z )
			{
				output[x + sizeX * z] = Sample( minX + x, minZ + z );
			}
	}
}
