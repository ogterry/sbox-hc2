using System;
using System.Text.Json.Serialization;

namespace Voxel.Modifications;

#nullable enable

[GameResource( "World Gen Feature", "feature", "Something that can be spawned by World Gen.", Icon = "home" )]
public sealed class WorldGenFeature : GameResource
{
	/// <summary>
	/// How far from sea level does this spawn, in voxels.
	/// </summary>
	public RangedFloat HeightRange { get; set; }

	/// <summary>
	/// Terrain type this spawns in, where 0 is plains, and ~0.25 is the start of the mountains.
	/// </summary>
	[JsonPropertyName( "BiomeRange" )]
	public RangedFloat TerrainRange { get; set; }

	/// <summary>
	/// If true, don't spawn this on a slope.
	/// </summary>
	public bool WantsFlatGround { get; set; }

	/// <summary>
	/// If true, this spawns in solid ground. Not implemented yet.
	/// </summary>
	public bool SpawnsInGround { get; set; }

	/// <summary>
	/// How many must we spawn in the world? Not implemented yet.
	/// </summary>
	public int MinCount { get; set; } = 0;

	/// <summary>
	/// What's the most we can spawn in the world? Use 0 for unlimited. Not implemented yet.
	/// </summary>
	public int MaxCount { get; set; } = 0;

	/// <summary>
	/// Avoid spawning other features closer than this distance.
	/// </summary>
	public float Radius { get; set; } = 512f;

	/// <summary>
	/// Relative frequency of this feature, default is 1.
	/// </summary>
	public float Weight { get; set; } = 1f;

	public delegate void SpawnDelegate( VoxelWorldGen worldGen, Vector3 origin );

	/// <summary>
	/// How do we spawn this object?
	/// </summary>
	public SpawnDelegate? Spawn { get; set; }
}

partial class VoxelWorldGen
{
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
			var sample = Heightmap[localPos.x, localPos.z];

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

		FlattenGroundComponent.Apply( Heightmap, go );

		if ( isNetworked && !Scene.IsEditor )
		{
			go.NetworkSpawn( null );
		}

		return go;
	}

	public void SpawnOreSeam( Block block, Vector3 position, RangedFloat sizeRange, RangedFloat depthRange )
	{
		throw new NotImplementedException( "Ore seams aren't implemented yet." );
	}
}

public record Heightmap( int Size, Transform Transform, HeightmapSample[] Samples )
{
	public Heightmap( int size, Transform transform )
		: this( size, transform, new HeightmapSample[size * size] )
	{

	}

	public HeightmapSample this[ int x, int z ]
	{
		get
		{
			x = Math.Clamp( x, 0, Size - 1 );
			z = Math.Clamp( z, 0, Size - 1 );

			return Samples[x + z * Size];
		}
		set
		{
			if ( x < 0 || x >= Size ) return;
			if ( z < 0 || z >= Size ) return;

			Samples[x + z * Size] = value;
		}
	}

	public HeightmapSample this[ Vector3 worldPos ]
	{
		get
		{
			var localPos = WorldToVoxelCoord( worldPos );
			return this[localPos.y, localPos.x];
		}
	}

	public Vector3Int WorldToVoxelCoord( Vector3 worldPos )
	{
		var localPos = Transform.PointToLocal( worldPos );
		return new Vector3Int( (int)localPos.y, (int)localPos.z, (int)localPos.x );
	}

	public Vector3 VoxelCoordToWorld( Vector3Int voxelCoord )
	{
		var localPos = new Vector3( voxelCoord.z, voxelCoord.x, voxelCoord.y );
		return Transform.PointToWorld( localPos );
	}
}
