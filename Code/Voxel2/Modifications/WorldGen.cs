using System;
using System.Text.Json.Serialization;
using HC2;
using Sandbox.Diagnostics;
using Sandbox.Utility;

namespace Voxel.Modifications;

#nullable enable

[Icon( "casino" )]
public sealed class VoxelWorldGen : Component, Component.ExecuteInEditor
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
			Log.Info( $"Applying heightmap! size: {heightmap.Length}" );
			VoxelNetworking.Modify( new HeightmapModification( 0, VoxelNetworking.Renderer.Size, heightmap ) );
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
	internal HeightmapSample[] Generate()
	{
		DestroyProps();

		Random = new Random( Seed );

		_size = VoxelNetworking.Renderer.Size.x;
		var heightmap = _heightmap = new HeightmapSample[_size * _size];

		var sampler = new WorldGenSampler( Parameters, Seed, _size );
		sampler.Sample( 0, 0, _size, _size, _heightmap );

		SpawnFeatures( Vector3.Zero, 4096f, true, Parameters.Features.ToArray() );

		Random = null!;
		_heightmap = null!;

		return heightmap;
	}

	private WorldGenFeature? GetRandomWeighted( IReadOnlyList<WorldGenFeature> features )
	{
		if ( features.Count == 0 )
		{
			return null;
		}

		var totalWeight = features.Sum( x => x.Weight );
		var index = Random.NextSingle() * totalWeight;

		foreach ( var feature in features )
		{
			index -= feature.Weight;

			if ( index <= 0f )
			{
				return feature;
			}
		}

		return null;
	}

	public Random Random { get; private set; } = null!;

	private HeightmapSample[] _heightmap = null!;
	private int _size;

	public HeightmapSample SampleHeightmap( int x, int z )
	{
		x = Math.Clamp( x, 0, _size - 1 );
		z = Math.Clamp( z, 0, _size - 1 );

		return _heightmap[x + z * _size];
	}

	public void SpawnFeatures( Vector3 origin, float radius, bool uniform = false, params WorldGenFeature[] features )
	{
		if ( features.Length == 0 ) return;

		var blocked = new List<Circle>();

		const int maxAttempts = 128;

		var maxSpawned = Math.Max( 10, radius / 4f );
		var attempts = 0;
		var spawned = 0;

		var minRadius = features.Min( x => x.Radius );

		bool AnyBlocking( Circle area, out float minDist )
		{
			// TODO: optimize!

			minDist = float.PositiveInfinity;

			foreach ( var circle in blocked )
			{
				var distSq = (circle.Center - area.Center).LengthSquared;
				var minRadSq = (circle.Radius + area.Radius) * (circle.Radius + area.Radius);

				if ( distSq <= minRadSq )
				{
					minDist = 0f;
					return true;
				}

				minDist = Math.Min( minDist, MathF.Sqrt( distSq ) - circle.Radius );
			}

			return false;
		}

		var filteredFeatures = new List<WorldGenFeature>();

		while ( spawned < maxSpawned && attempts++ < maxAttempts )
		{
			var pos2d = (Vector2)origin + (uniform ? Random.VectorInCircle( radius - minRadius ) : Random.Gaussian2D( 0f, radius / 3f ));

			if ( AnyBlocking( new Circle( pos2d, minRadius ), out float minDist ) ) continue;

			var localPos = Renderer.WorldToVoxelCoords( pos2d );
			var sample = SampleHeightmap( localPos.x, localPos.z );

			localPos.y = sample.Height;

			filteredFeatures.Clear();

			foreach ( var feature in features )
			{
				if ( feature.Radius >= minDist ) continue;
				if ( feature.HeightRange.x > sample.Height || feature.HeightRange.y < sample.Height ) continue;
				if ( feature.TerrainRange.x > sample.Terrain || feature.TerrainRange.y < sample.Terrain ) continue;

				filteredFeatures.Add( feature );
			}

			var selected = GetRandomWeighted( filteredFeatures );

			if ( selected is null ) continue;

			attempts = 0;
			spawned++;

			blocked.Add( new Circle( pos2d, selected.Radius ) );

			selected.Spawn?.Invoke( this, new Vector3( pos2d.x, pos2d.y, sample.Height * 16f ) );
		}
	}

	public Prop? SpawnProp( Model model, Vector3 position, float scale = 1f )
	{
		var go = new GameObject( true, model.ResourceName )
		{
			Transform = {
				Position = position.SnapToGrid( 4f ),
				Rotation = Rotation.FromYaw( Random.Next( 0, 4 ) * 90f ),
				Scale = scale
			}
		};

		_spawnedObjects.Add( go );

		var prop = go.Components.Create<Prop>();

		prop.IsStatic = true;
		prop.Model = model;

		go.Flags |= GameObjectFlags.NotSaved;

		return prop;
	}

	public GameObject? SpawnSpawnPoint( Vector3 position )
	{
		var go = new GameObject( true );
		go.Name = "Spawn Point";
		go.Transform.Position = position;
		go.Components.Create<SpawnPoint>();
		go.Flags |= GameObjectFlags.NotSaved;
		_spawnedObjects.Add( go );

		if ( !Scene.IsEditor )
		{
			go.NetworkSpawn( null );
		}

		return go;
	}

	public GameObject? SpawnPrefab( PrefabFile prefab, Vector3 position )
	{
		// Sample random even if we don't spawn, to keep things deterministic

		var yaw = Rotation.FromYaw( Random.Next( 0, 4 ) * 90f );
		var isNetworked = Game.IsPlaying && prefab.RootObject["NetworkMode"]?.GetValue<int>() == (int)NetworkMode.Object;

		if ( IsProxy && isNetworked )
		{
			return null;
		}

		var go = GameObject.Clone( prefab, new Transform( position.SnapToGrid( 4f ), yaw ) );

		_spawnedObjects.Add( go );

		go.Flags |= GameObjectFlags.NotSaved;

		if ( isNetworked && !Scene.IsEditor )
		{
			go.NetworkSpawn( null );
		}

		return go;
	}

	public void SpawnOreSeam( Block block, Vector3 position, RangedFloat sizeRange, RangedFloat depthRange )
	{

	}
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

[GameResource( "World Gen Feature", "feature", "Something that can be spawned by World Gen.", Icon = "home" )]
public sealed class WorldGenFeature : GameResource
{
	public RangedFloat HeightRange { get; set; }

	[JsonPropertyName( "BiomeRange" )]
	public RangedFloat TerrainRange { get; set; }

	/// <summary>
	/// If true, this spawns in solid ground.
	/// </summary>
	public bool SpawnsInGround { get; set; }

	public float Radius { get; set; } = 512f;
	public float Weight { get; set; } = 1f;

	public delegate void SpawnDelegate( VoxelWorldGen worldGen, Vector3 origin );

	public SpawnDelegate? Spawn { get; set; }
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
