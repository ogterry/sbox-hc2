using Sandbox.Diagnostics;
using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Voxel;
using Voxel.Modifications;

namespace HC2;

public interface IObjectSaveData
{
	/// <summary>
	/// So we can access the GameObject
	/// </summary>
	[JsonIgnore]
	public GameObject GameObject { get; }

	public string Save();
	public void Load( string data );
}

/// <summary>
/// The state of the voxel world.
/// </summary>
public record VoxelWorldState( int Version, int Seed, string ParametersPath, Vector3Int Size, IReadOnlyList<ChunkState> Chunks )
{
	public const int CurrentVersion = 5;
}

public struct ChunkState
{
	public ChunkState( Vector3Int index, byte[] data )
	{
		Index = index;
		Data = data;
	}

	public Vector3Int Index { get; set; }
	public byte[] Data { get; set; }
}

/// <summary>
/// The state of all the objects in the world, this'll be NPCs, doors, etc..
/// </summary>
public struct WorldObjectState
{
	public WorldObjectState()
	{
	}

	/// <summary>
	/// Currently, we're going to support prefab objects only. 
	/// Serializing a bunch of components and their properties seems pointless when we have the data right there.
	/// </summary>
	public struct Object
	{
		public string PrefabFilePath { get; set; }
		public Vector3 Position { get; set; }
		public Angles Angles { get; set; }

		/// <summary>
		/// A string of serialized data that's defined by the type, using <see cref="IObjectSaveData"/>
		/// </summary>
		public string Data { get; set; }

		public static Object? From( IObjectSaveData saveable )
		{
			var go = saveable.GameObject;

			if ( !go.IsValid() ) return null;

			// TODO: fix this in engine
			var comp = go.Components.Get<GetPrefabSource>( FindMode.EverythingInSelf );
			var prefabFilePath = comp.IsValid() ? comp.PrefabSource : go.PrefabInstanceSource;

			if ( string.IsNullOrEmpty( prefabFilePath ) )
				return null;

			return new Object()
			{
				PrefabFilePath = prefabFilePath,
				Position = go.Transform.Position,
				Angles = go.Transform.Rotation.Angles(),
				Data = saveable.Save()
			};
		}

		/// <summary>
		/// Create a GameObject from a saved object.
		/// </summary>
		/// <returns></returns>
		public GameObject CreateGameObject()
		{
			var prefabFile = ResourceLibrary.Get<PrefabFile>( this.PrefabFilePath );
			var prefabScene = SceneUtility.GetPrefabScene( prefabFile );

			var inst = prefabScene.Clone( new CloneConfig()
			{
				Transform = new Transform().WithPosition( this.Position ).WithRotation( this.Angles.ToRotation() ),
				StartEnabled = true,
			} );

			// Load the serialized data
			var save = inst.Components.Get<IObjectSaveData>();
			save.Load( this.Data );

			return inst;
		}
	}

	/// <summary>
	/// The list of objects.
	/// </summary>
	public List<Object> Objects { get; set; } = new();
}

public class WorldSave
{
	[JsonIgnore]
	public string FilePath { get; set; } = null;

	[JsonIgnore]
	public Guid Id { get; set; } = Guid.Empty;

	/// <summary>
	/// What's the target version?
	/// </summary>
	public const int CurrentVersion = VoxelWorldState.CurrentVersion;

	/// <summary>
	/// Is this world outdated? This means we updated something that's not backwards compatible.
	/// </summary>
	[JsonIgnore]
	public bool IsOutdated => Version < CurrentVersion;

	/// <summary>
	/// What's the version of this save? We'll use it to mark incompatibility.
	/// </summary>
	public int Version { get; set; } = CurrentVersion;

	/// <summary>
	/// A friendly name that we'll decide in the UI most probably.
	/// </summary>
	public string Name { get; set; } = "My World";

	/// <summary>
	/// When was this world created?
	/// </summary>
	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

	/// <summary>
	/// When was this world last saved?
	/// </summary>
	public DateTimeOffset LastSaved { get; set; } = DateTimeOffset.UtcNow;

	/// <summary>
	/// The state of the voxel world.
	/// </summary>
	public VoxelWorldState WorldState { get; set; }

	/// <summary>
	/// The state of all transient objects in the world. Can save extra data on them.
	/// </summary>
	public WorldObjectState ObjectState { get; set; }

	/// <summary>
	/// Retrieves a WorldSave from path.
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public static WorldSave Get( string path )
	{
		var worldSave = FileSystem.Data.ReadJson<WorldSave>( path );
		if ( worldSave is not null )
		{
			if ( worldSave.IsOutdated )
			{
				Log.Warning( $"{worldSave} is outdated... we won't allow loading it" );
			}

			worldSave.FilePath = path;

			// TODO: this is poo
			worldSave.Id = Guid.Parse( path.Replace( ".json", "" ).Replace( "worlds/", "" ) );

			return worldSave;
		}
		else
		{
			Log.Warning( $"Couldn't load world from file... {path}" );
			return null;
		}
	}

	/// <summary>
	/// Gets a list of worlds from the worlds folder
	/// </summary>
	/// <returns></returns>
	public static List<WorldSave> GetAll()
	{
		var files = FileSystem.Data.FindFile( "worlds", "*.json" );
		var saves = new List<WorldSave>();

		foreach ( var file in files )
		{
			var save = Get( $"worlds/{file}" );
			if ( save is not null )
			{
				saves.Add( save );
			}
		}

		return saves;
	}

	public override string ToString()
	{
		return $"World: {Name}";
	}
}

public sealed class WorldPersistence : Component
{
	/// <summary>
	/// This is the file to load when we join, as a host.. this is selected in the game UI.
	/// </summary>
	public static string FileToLoad { get; set; } = null;

	/// <summary>
	/// Singleton
	/// </summary>
	public static WorldPersistence Instance { get; private set; }

	/// <summary>
	/// This is shitty but should work
	/// </summary>
	public static async Task TryLoadWorld()
	{
		var persistence = Game.ActiveScene.GetAllComponents<WorldPersistence>().FirstOrDefault();
		var world = persistence.GetVoxelWorld();
		var worldGen = world.Components.Get<VoxelWorldGen>();

		if ( Sandbox.Networking.IsHost )
		{
			if ( !persistence.TryLoadFromSelectedFile() )
			{
				worldGen.Randomize();
			}
		}
		else if ( LoadedWorldState is not null )
		{
			await persistence.LoadWorldState( LoadedWorldState );
		}
	}
	
	public static VoxelWorldState LoadedWorldState { get; private set; }

	public static void WriteWorldState( ref ByteStream bs )
	{
		var persistence = Game.ActiveScene.GetAllComponents<WorldPersistence>().FirstOrDefault();
		var world = persistence.GetVoxelWorld();
		var worldGen = world.Components.Get<VoxelWorldGen>();

		// We have a world state to write.
		bs.Write( true );
		bs.Write( VoxelWorldState.CurrentVersion );
		
		var seed = worldGen?.Seed ?? 0;
		var path = worldGen?.Parameters?.ResourcePath;
		var size = world.Size;

		var chunkArray = world.Model.Chunks.Where( x => x is { Allocated: true } ).Select( persistence.SerializeChunk )
			.ToArray();

		bs.Write( seed );
		bs.Write( path ?? string.Empty );
		bs.Write( size );
		bs.Write( chunkArray.Length );

		for ( var i = 0; i < chunkArray.Length; i++ )
		{
			var chunk = chunkArray[i];
			bs.Write( chunk.Index );
			bs.Write( chunk.Data.Length );
			bs.Write( chunk.Data );
		}
	}

	public static void ReadWorldState( ref ByteStream bs )
	{
		var hasVoxelWorld = bs.Read<bool>();
		if ( !hasVoxelWorld ) return;

		var version = bs.Read<int>();
		var seed = bs.Read<int>();
		var parametersPath = bs.Read<string>();
		var size = bs.Read<Vector3Int>();
		var chunkCount = bs.Read<int>();
		var chunks = new ChunkState[chunkCount];
		
		for ( var i = 0; i < chunkCount; i++ )
		{
			var chunk = new ChunkState();
			chunk.Index = bs.Read<Vector3Int>();
			var dataCount = bs.Read<int>();
			chunk.Data = bs.ReadArray<byte>( dataCount ).ToArray();
			chunks[i] = chunk;
		}

		LoadedWorldState = new( version, seed, parametersPath, size, chunks.AsReadOnly() );
	}

	protected override void OnStart()
	{
		Instance = this;
	}

	/// <summary>
	/// We might not need this, but yeah..
	/// </summary>
	[HostSync]
	public string WorldName { get; set; } = "My World";

	/// <summary>
	/// When we load a save, we'll set this, so other players know what the map's guid is, for character location persistence.
	/// </summary>
	[HostSync]
	public Guid CurrentSave { get; set; }

	/// <summary>
	/// Try to load a world from <see cref="FileToLoad"/>, which is dictated through our UI flow.
	/// </summary>
	/// <returns></returns>
	private bool TryLoadFromSelectedFile()
	{
		// We'll just generate something 
		if ( string.IsNullOrEmpty( FileToLoad ) )
			return false;

		return LoadFromFile( FileToLoad );
	}

	/// <summary>
	/// Loads a world from any file
	/// </summary>
	/// <param name="path"></param>
	public bool LoadFromFile( string path )
	{
		Log.Info( $"Trying to load world from file {path}" );

		var save = WorldSave.Get( path );
		if ( save is not null )
		{
			return Load( save );
		}

		return false;
	}

	/// <summary>
	/// Destroys any saveable objects that might exist in the world already.
	/// </summary>
	private void DestroyAnyTransientObjects()
	{
		foreach ( var obj in Scene.GetAllComponents<IObjectSaveData>() )
		{
			obj.GameObject.Destroy();
		}
	}

	/// <summary>
	/// Loads a world from a save file.
	/// </summary>
	/// <param name="save"></param>
	public bool Load( WorldSave save )
	{
		// Host only
		if ( !Sandbox.Networking.IsHost )
		{
			Log.Warning( "Tried to load a world save but we're not the host.." );
			return false;
		}

		// Don't allow loading outdated saves.
		if ( save.IsOutdated )
		{
			return false;
		}

		// Mark as current save
		CurrentSave = save.Id;
		WorldName = save.Name;

		Log.Info( "Trying to load a world save..." );

		DestroyAnyTransientObjects();

		// Load the world from the save file
		_ = LoadWorldState( save.WorldState );

		// Create GameObjects for every saved state GameObject
		foreach ( var obj in save.ObjectState.Objects )
		{
			var go = obj.CreateGameObject();
			go.NetworkSpawn( null );
		}

		return true;
	}

	[Button( "Save World To File" )]
	public WorldSave SaveToFile()
	{
		if ( !FileSystem.Data.DirectoryExists( "worlds" ) )
			FileSystem.Data.CreateDirectory( "worlds" );

		var guid = CurrentSave;
		if ( guid == Guid.Empty ) guid = Guid.NewGuid();
		CurrentSave = guid;

		var save = PackWorld( WorldName );
		var fileName = $"worlds/{guid}.json";
		var json = Json.Serialize( save );

		FileToLoad = fileName;

		FileSystem.Data.WriteAllText( fileName, json );

		return save;
	}

	/// <summary>
	/// Run through all of our saveable objects and save them!
	/// </summary>
	/// <returns></returns>
	public WorldObjectState SaveObjectState()
	{
		var list = new List<WorldObjectState.Object>();

		foreach ( var obj in Scene.GetAllComponents<IObjectSaveData>() )
		{
			var savedObj = WorldObjectState.Object.From( obj );

			if ( savedObj.HasValue )
				list.Add( savedObj.Value );
		}

		return new WorldObjectState()
		{
			Objects = list
		};
	}

	private VoxelRenderer GetVoxelWorld()
	{
		var world = Scene.GetAllComponents<VoxelRenderer>()
			.FirstOrDefault( x => x.Tags.Has( "terrain" ) );

		if ( world is null )
		{
			Log.Warning( $"Couldn't find the world {nameof( VoxelRenderer )} (expected tag \"terrain\")" );
			return null;
		}

		return world;
	}

	/// <summary>
	/// Save the voxel world state into something that's readable 
	/// </summary>
	/// <returns></returns>
	public VoxelWorldState SaveWorldState()
	{
		var world = GetVoxelWorld();
		var worldGen = world.Components.Get<VoxelWorldGen>();

		return new( VoxelWorldState.CurrentVersion,
			worldGen?.Seed ?? 0,
			worldGen?.Parameters?.ResourcePath,
			world.Size, world.Model.Chunks.Where( x => x is { Allocated: true } )
			.Select( SerializeChunk )
			.ToArray() );
	}

	public async Task LoadWorldState( VoxelWorldState state )
	{
		if ( state is null ) return;
		if ( state.Version < 2 ) throw new NotImplementedException();

		var world = GetVoxelWorld();
		var worldGen = world.Components.Get<VoxelWorldGen>();
		worldGen.Seed = state.Seed;
		worldGen.Parameters = ResourceLibrary.Get<WorldGenParameters>( state.ParametersPath );
		
		Assert.AreEqual( world.Size, state.Size );

		// Wait until the world is ready
		while ( !world.IsReady )
		{
			await Task.Yield();
		}

		foreach ( var chunk in world.Model.Chunks )
		{
			chunk.Deallocate();
		}

		foreach ( var chunkState in state.Chunks )
		{
			var chunk = world.Model.InitChunkLocal( chunkState.Index.x, chunkState.Index.y, chunkState.Index.z );

			DeserializeChunk( chunk, chunkState );
			chunk.SetDirty();
		}

		world.MeshChunks();

		// Spawn props afterwards, this won't modify the voxel world itself
		worldGen.Generate();
	}

	private static void WriteVarUshort( ref ByteStream writer, ushort value )
	{
		Assert.True( value < 0x8000 );

		if ( value < 0x80 )
		{
			writer.Write( (byte)value );
			return;
		}

		// 8th bit indicates there's a second byte

		writer.Write( (byte)((value & 0x7f) | 0x80) );
		writer.Write( (byte)(value >> 7) );
	}

	private static ushort ReadVarUshort( ref ByteStream reader )
	{
		var lower = reader.Read<byte>();
		if ( lower < 0x80 ) return lower;

		var upper = reader.Read<byte>();
		return (ushort)((lower & 0x7f) | (upper << 7));
	}

	internal ChunkState SerializeChunk( Chunk chunk )
	{
		Assert.True( chunk.Allocated );

		var writer = ByteStream.Create( 4096 );

		// Just a basic RLE for now

		try
		{
			var voxels = chunk.Voxels;

			var prev = 0;
			var count = 0;

			for ( var x = 0; x < Constants.ChunkSize; ++x )
				for ( var z = 0; z < Constants.ChunkSize; ++z )
					for ( var y = 0; y < Constants.ChunkSize; ++y )
					{
						var next = voxels[Chunk.GetAccessLocal( x, y, z )];

						if ( next != prev )
						{
							// Count will always be < 32 * 32 * 32 = (32,768)

							WriteVarUshort( ref writer, (ushort)count );
							writer.Write( next );

							prev = next;
							count = 1;
						}
						else
						{
							count++;
						}
					}

			return new(
				new( chunk.ChunkPosX, chunk.ChunkPosY, chunk.ChunkPosZ ),
				writer.ToArray() );
		}
		finally
		{
			writer.Dispose();
		}
	}

	private static void SetVoxelRun( byte[] voxels, ref int index, byte value, int count )
	{
		for ( var i = 0; i < count; ++i, ++index )
		{
			// Definitely a way to simplify this

			var y = index & Constants.ChunkMask;
			var z = (index >> Constants.ChunkShift) & Constants.ChunkMask;
			var x = (index >> Constants.ChunkShift >> Constants.ChunkShift) & Constants.ChunkMask;

			voxels[Chunk.GetAccessLocal( x, y, z )] = value;
		}
	}

	private void DeserializeChunk( Chunk chunk, ChunkState state )
	{
		Assert.True( chunk.Allocated );

		var reader = ByteStream.CreateReader( state.Data );

		try
		{
			var voxels = chunk.Voxels;

			var prev = (byte)0;
			var index = 0;

			while ( reader.ReadRemaining > 0 )
			{
				SetVoxelRun( voxels, ref index, prev, ReadVarUshort( ref reader ) );
				prev = reader.Read<byte>();
			}

			SetVoxelRun( voxels, ref index, prev, Constants.ChunkSizeCubed - index );
		}
		finally
		{
			reader.Dispose();
		}

		chunk.UpdateHeightmaps();
	}

	/// <summary>
	/// Pack the world into a <see cref="WorldSave"/>
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public WorldSave PackWorld( string name = "My World" )
	{
		WorldName = name;

		var save = new WorldSave()
		{
			Name = name,
			ObjectState = SaveObjectState(),
			WorldState = SaveWorldState()
		};

		return save;
	}
}
