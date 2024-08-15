using System;
using HC2;
using Sandbox.Diagnostics;
using Sandbox.Utility;

namespace Voxel.Modifications;

#nullable enable

public record WorldGenModification( int Seed, WorldGenParameters Parameters, Vector3Int Min, Vector3Int Max ) : IModification
{
	public ModificationKind Kind => ModificationKind.WorldGen;

	public WorldGenModification( ByteStream stream )
		: this(
			stream.Read<int>(),
			ResourceLibrary.Get<WorldGenParameters>( stream.Read<int>() ),
			stream.Read<Vector3Int>(),
			stream.Read<Vector3Int>() )
	{

	}

	private HeightmapSample[]? _worldHeightmap;
	private WorldGenSampler? _worldSampler;

	[ThreadStatic]
	private static HeightmapSample[]? _chunkHeightmap;

	private WorldGenSampler WorldSampler => _worldSampler ??= new WorldGenSampler( Parameters, Seed, Max.x - Min.x );

	public bool ShouldCreateChunk( Vector3Int chunkMin )
	{
		var chunkHeightmap = _chunkHeightmap ??= new HeightmapSample[Constants.ChunkSizeSquared];

		// Span<HeightmapSample> chunkHeightmap = stackalloc HeightmapSample[Constants.ChunkSizeSquared];

		GetChunkHeightMap( chunkMin.x, chunkMin.z, chunkHeightmap );

		return IsChunkPopulated( chunkMin, chunkHeightmap );
	}

	private bool IsChunkPopulated( Vector3Int chunkMin, Span<HeightmapSample> chunkHeightmap )
	{
		var maxHeight = 0;

		for ( var i = 0; i < Constants.ChunkSizeSquared; ++i )
		{
			maxHeight = Math.Max( maxHeight, chunkHeightmap[i].Height );
		}

		return maxHeight > chunkMin.y;
	}

	private void UpdateHeightmap()
	{
		if ( _worldHeightmap is not null ) return;

		_worldHeightmap = new HeightmapSample[(Max.x - Min.x) * (Max.z - Min.z)];
		WorldSampler.Sample( Min.x, Min.z, Max.x, Max.z, _worldHeightmap );
	}

	private void GetChunkHeightMap( int minX, int minZ, Span<HeightmapSample> output )
	{
		UpdateHeightmap();

		Assert.True( output.Length >= Constants.ChunkSizeSquared );
		Assert.True( minX >= Min.x );
		Assert.True( minZ >= Min.z );
		Assert.True( minX + Constants.ChunkSize <= Max.x );
		Assert.True( minZ + Constants.ChunkSize <= Max.z );

		var stride = Max.x - Min.x;
		var baseIndex = minX - Min.x + (minZ - Min.z) * stride;

		for ( var z = 0; z < Constants.ChunkSize; ++z )
		{
			var index = baseIndex + z * stride;

			_worldHeightmap
				.AsSpan( index, Constants.ChunkSize )
				.CopyTo( output.Slice( z << Constants.ChunkShift, Constants.ChunkSize ) );
		}
	}

	public void Write( ref ByteStream stream )
	{
		stream.Write( Seed );
		stream.Write( Parameters.ResourceId );
		stream.Write( Min );
		stream.Write( Max );
	}

	public void Apply( VoxelRenderer renderer, Chunk chunk )
	{
		var chunkHeightmap = _chunkHeightmap ??= new HeightmapSample[Constants.ChunkSizeSquared];
		// Span<HeightmapSample> chunkHeightmap = stackalloc HeightmapSample[Constants.ChunkSizeSquared];

		var min = chunk.WorldMin;

		GetChunkHeightMap( min.x, min.z, chunkHeightmap );

		if ( !IsChunkPopulated( min, chunkHeightmap ) )
		{
			chunk.Deallocate();
			return;
		}
		
		chunk.Clear();

		var voxels = chunk.Voxels;
		var biomeSampler = renderer.Components.Get<BiomeSampler>();
		var palette = renderer.Palette;

		for ( var x = 0; x < Constants.ChunkSize; x++ )
		{
			for ( var z = 0; z < Constants.ChunkSize; z++ )
			{
				var i = Chunk.WorldToLocal( x );
				var k = Chunk.WorldToLocal( z );

				var heightmapAccess = Chunk.GetHeightmapAccess( i, k );

				var (height, terrain) = chunkHeightmap[heightmapAccess];
				var biome = biomeSampler?.GetBiomeAt( min.x + x, min.z + z );

				var minJ = int.MaxValue;
				var maxJ = int.MinValue;

				var localHeight = height - min.y;

				for ( var y = 0; y < Constants.ChunkSize && y < localHeight; y++ )
				{
					byte blockIndex = 1;

					if ( biome?.GetBlock( height, terrain, localHeight - y - 1 ) is { } block )
					{
						blockIndex = palette.GetBlockIndex( block, block.MaxHealth );
					}

					var j = Chunk.WorldToLocal( y );

					voxels[Chunk.GetAccessLocal( i, j, k )] = blockIndex;

					minJ = Math.Min( j, minJ );
					maxJ = Math.Max( j, maxJ );
				}

				if ( maxJ < 0 || minJ > 255 ) continue;

				chunk.MinAltitude[heightmapAccess] = (byte)minJ;
				chunk.MaxAltitude[heightmapAccess] = (byte)maxJ;
			}
		}
	}
}

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

		if ( WorldPersistence.FileToLoad is null )
		{
			Log.Info( $"Generating!" );
			VoxelNetworking.Modify( new WorldGenModification( Seed, Parameters, 0, VoxelNetworking.Renderer.Size ) );
		}

		Log.Info( $"Spawning props!" );
		SpawnProps();
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

	internal void SpawnProps()
	{
		DestroyProps();

		if ( Parameters is null || !Renderer.IsValid() )
		{
			return;
		}

		Random = new Random( Seed );
		Sampler = new WorldGenSampler( Parameters, Seed, Renderer.Size.x );

		SpawnFeatures( Vector3.Zero, 4096f, true, Parameters.Features.ToArray() );

		Random = null!;
		Sampler = null!;
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
	public WorldGenSampler Sampler { get; private set; } = null!;

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
			var sample = Sampler.Sample( localPos.x, localPos.z );

			localPos.y = sample.Height;

			filteredFeatures.Clear();

			foreach ( var feature in features )
			{
				if ( feature.Radius >= minDist ) continue;
				if ( feature.HeightRange.x > sample.Height || feature.HeightRange.y < sample.Height ) continue;
				if ( feature.BiomeRange.x > sample.Terrain || feature.BiomeRange.y < sample.Terrain ) continue;

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
			go.NetworkSpawn();
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
			go.NetworkSpawn();
		}

		return go;
	}
}

[GameResource( "World Gen Parameters", "worldgen", "Parameters for the voxel world generator.", Icon = "public" )]
public sealed class WorldGenParameters : GameResource
{
	public Curve TerrainBias { get; set; }
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

		return new ( height, terrainNoise );
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
