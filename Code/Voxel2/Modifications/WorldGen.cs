using System;
using Sandbox;
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

	public void Apply( Scene scene, Chunk chunk )
	{
		var min = chunk.WorldMin;
		var random = new Random( Seed );
		var resource = ResourceLibrary.Get<WorldGenParameters>( ResourceId );

		var sampler = new WorldGenSampler( resource, Seed, Max.x - Min.x );
		var biomeSampler = scene.GetAllComponents<BiomeSampler>().FirstOrDefault();

		chunk.Clear();

		var voxels = chunk.Voxels;

		for ( var x = 0; x < Constants.ChunkSize; x++ )
		{
			for ( var z = 0; z < Constants.ChunkSize; z++ )
			{
				var (terrain, height) = sampler.Sample( min.x + x, min.z + z );
				var biome = biomeSampler?.GetBiomeAt( min.x + x, min.z + z );

				height -= min.y;

				var i = Chunk.WorldToLocal( x );
				var k = Chunk.WorldToLocal( z );

				var minJ = int.MaxValue;
				var maxJ = int.MinValue;

				var soilDepth = (int) (8f - terrain * 16f);

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
	[RequireComponent] public VoxelRenderer Renderer { get; private set; }

	[Property]
	public int Seed { get; set; }

	[Property]
	public WorldGenParameters Parameters { get; set; }

	private readonly List<GameObject> _spawnedObjects = new();

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

		if ( IsProxy ) return;

		SpawnProps();
	}

	protected override void OnEnabled()
	{
		Regenerate();
	}

	private void DestroyProps()
	{
		foreach ( var spawnedObject in _spawnedObjects )
		{
			spawnedObject.Destroy();
		}

		_spawnedObjects.Clear();
	}

	private readonly record struct Circle( Vector2 Center, float Radius );

	private void SpawnProps()
	{
		DestroyProps();

		if ( Parameters is null || !Renderer.IsValid() )
		{
			return;
		}

		Random = new Random( Seed );
		Sampler = new WorldGenSampler( Parameters, Seed, Renderer.Size.x );

		SpawnFeatures( Vector3.Zero, 4096f, Parameters.Features.ToArray() );

		Random = null;
		Sampler = null;
	}

	private WorldGenFeature GetRandomWeighted( IReadOnlyList<WorldGenFeature> features )
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

	public Random Random { get; private set; }
	public WorldGenSampler Sampler { get; private set; }

	private void SpawnFeatures( Vector3 origin, float radius, params WorldGenFeature[] features )
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
			var pos2d = (Vector2)origin + Random.VectorInCircle( radius - minRadius );

			if ( AnyBlocking( new Circle( pos2d, minRadius ), out float minDist ) ) continue;

			var localPos = Renderer.WorldToVoxelCoords( pos2d );
			var sample = Sampler.Sample( localPos.x, localPos.z );

			localPos.y = sample.Height;

			Log.Info( localPos );

			filteredFeatures.Clear();

			foreach ( var feature in features )
			{
				if ( feature.Radius >= minDist )
				{
					Log.Info( "Too close!" );
					continue;
				}

				if ( feature.HeightRange.x > sample.Height || feature.HeightRange.y < sample.Height )
				{
					Log.Info( $"out of height range!" );
					continue;
				}

				if ( feature.BiomeRange.x > sample.Biome || feature.BiomeRange.y < sample.Biome )
				{
					Log.Info( "out of biome range!" );
					continue;
				}

				filteredFeatures.Add( feature );
			}

			var selected = GetRandomWeighted( filteredFeatures );

			if ( selected is null ) continue;

			attempts = 0;
			spawned++;

			blocked.Add( new Circle( pos2d, selected.Radius ) );

			selected.Spawn( this, new Vector3( pos2d.x, pos2d.y, sample.Height * 16f ) );
		}
	}

	public Prop SpawnProp( Model model, Vector3 position, float scale = 1f )
	{
		var go = new GameObject( true, "Prop" )
		{
			Transform = {
				Position = position.SnapToGrid( 4f ),
				Rotation = Rotation.FromYaw( Random.Shared.Next( 0, 4 ) * 90f ),
				Scale = scale
			},
			Flags = GameObjectFlags.NotSaved
		};

		_spawnedObjects.Add( go );

		var prop = go.Components.Create<Prop>();

		prop.IsStatic = true;
		prop.Model = model;

		go.NetworkSpawn();

		return prop;
	}

	public GameObject SpawnPrefab( PrefabFile prefab, Vector3 position )
	{
		var go = GameObject.Clone( prefab, new Transform( position.SnapToGrid( 4f ), Rotation.FromYaw( Random.Shared.Next( 0, 4 ) * 90f ) ) );

		_spawnedObjects.Add( go );

		go.NetworkSpawn();

		return go;
	}
}

[GameResource( "World Gen Parameters", "worldgen", "Parameters for the voxel world generator.", Icon = "public" )]
public sealed class WorldGenParameters : GameResource
{
	public Curve PlainsHeight { get; set; }
	public Curve MountainsHeight { get; set; }

	public List<WorldGenFeature> Features { get; set; } = new();
}

[GameResource( "World Gen Feature", "feature", "Something that can be spawned by World Gen.", Icon = "home" )]
public sealed class WorldGenFeature : GameResource
{
	public RangedFloat HeightRange { get; set; }
	public RangedFloat BiomeRange { get; set; }

	public float Radius { get; set; } = 512f;
	public float Weight { get; set; } = 1f;


	public delegate void SpawnDelegate( VoxelWorldGen worldGen, Vector3 origin );

	public SpawnDelegate Spawn { get; set; }
}

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

	private readonly Curve _plainsHeight;
	private readonly Curve _mountainsHeight;

	public WorldGenSampler( WorldGenParameters parameters, int seed, int worldSize )
	{
		var random = new Random( seed );

		_worldSize = worldSize;

		_biomeNoiseTransform = GetRandomNoiseTransform( random, 0.2f );
		_heightNoiseTransform = GetRandomNoiseTransform( random, 0.4f );

		_plainsHeight = parameters.PlainsHeight;
		_mountainsHeight = parameters.MountainsHeight;
	}

	public (float Biome, int Height) Sample( int x, int y )
	{
		var center = _worldSize >> 1;

		var centerDistSq = (x - center) * (x - center) + (y - center) * (y - center);
		var centrality = Math.Clamp( 4f - 4f * MathF.Sqrt( centerDistSq ) / center, 0f, 1f );

		var heightNoisePos = _heightNoiseTransform.PointToWorld( new Vector3( x, y, 0f ) );
		var heightNoise = Math.Clamp( Noise.Fbm( 4, heightNoisePos.x, heightNoisePos.y, heightNoisePos.z ), 0f, 1f ) * centrality;

		var biomeNoisePos = _biomeNoiseTransform.PointToWorld( new Vector3( x, y, 0f ) );
		var biomeNoise = Math.Clamp( Noise.Fbm( 3, biomeNoisePos.x, biomeNoisePos.y, biomeNoisePos.z ) * 2f - 0.5f, 0f, 1f );

		biomeNoise = MathF.Pow( biomeNoise, 4f ) * centrality;

		var plainsHeight = _plainsHeight.Evaluate( heightNoise );
		var mountainsHeight = _mountainsHeight.Evaluate( heightNoise );

		var height = (int)(plainsHeight + (mountainsHeight - plainsHeight) * biomeNoise);

		return (biomeNoise, height);
	}
}
