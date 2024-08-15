using Sandbox.Diagnostics;
using System;
using System.Text.Json.Serialization;
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
	public const int CurrentVersion = 2;
}

public struct ChunkState
{
	public ChunkState( Vector3Int index, string data )
	{
		Index = index;
		Data = data;
	}

	public Vector3Int Index { get; set; }
	public string Data { get; set; }
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
}

public sealed class WorldPersistence : Component
{
	/// <summary>
	/// This is the file to load when we join, as a host.. this is selected in the game UI.
	/// </summary>
	public static string FileToLoad { get; set; } = null;

	/// <summary>
	/// This is shitty but should work
	/// </summary>
	public static void TrySendVoxelWorld( Connection channel )
	{
		// We're not sending this to the host
		if ( channel.IsHost )
			return;

		// We must BE the host
		if ( !Sandbox.Networking.IsHost )
			return;

		// Send to our target
		using ( Rpc.FilterInclude( channel ) )
		{
			var persistence = Game.ActiveScene.GetAllComponents<WorldPersistence>().FirstOrDefault();

			var world = persistence.GetVoxelWorld();
			var worldGen = world.Components.Get<VoxelWorldGen>();
			var seed = worldGen?.Seed ?? 0;
			var path = worldGen?.Parameters?.ResourcePath;
			var size = world.Size;

			var chunkArray = world.Model.Chunks.Where( x => x is { Allocated: true } ).Select( persistence.SerializeChunk )
				.ToArray();

			Log.Info( $"Sending chunks from host to a client: {chunkArray.Count()}" );

			SendVoxelWorldRpc( seed, path, size, chunkArray );
		}
	}

	/// <summary>
	/// RPC over the voxel world state to a client
	/// </summary>
	/// <param name="seed"></param>
	/// <param name="parametersPath"></param>
	/// <param name="size"></param>
	/// <param name="states"></param>
	[Broadcast]
	private static void SendVoxelWorldRpc( int seed, string parametersPath, Vector3Int size, ChunkState[] states )
	{
		var version = VoxelWorldState.CurrentVersion;
		var persistence = Game.ActiveScene.GetAllComponents<WorldPersistence>().FirstOrDefault();

		Log.Info( $"Received voxel world information from the host:\n\tseed:{seed}\n\tchunks:{states.Count()}\n\tsize:{size}" );

		persistence.LoadWorldState( new( version, seed, parametersPath, size, states.AsReadOnly() ) );
	}

	protected override void OnStart()
	{
		TryLoadFromSelectedFile();
	}

	[HostSync]
	public string WorldName { get; set; } = "My World";
	[HostSync] 
	public Guid CurrentSave { get; set; }

	public static List<WorldSave> GetWorlds()
	{
		var files = FileSystem.Data.FindFile( "worlds", "*.json" );
		var saves = new List<WorldSave>();
		foreach ( var file in files )
		{
			var save = FileSystem.Data.ReadJson<WorldSave>( $"worlds/{file}" );
			save.FilePath = $"worlds/{file}";
			save.Id = new Guid( file.Replace( ".json", "" ) );
			saves.Add( save );
		}

		return saves;
	}

	private void TryLoadFromSelectedFile()
	{
		// We'll just generate something 
		if ( string.IsNullOrEmpty( FileToLoad ) )
		{
			return;
		}

		LoadFromFile( FileToLoad, true );
	}

	public void LoadFromFile( string path, bool refreshSnapshot = true )
	{
		Log.Info( $"Trying to load world from file {path}" );
		var worldSave = FileSystem.Data.ReadJson<WorldSave>( path );
		if ( worldSave is not null )
		{
			Load( worldSave );
		}
		else
		{
			Log.Warning( $"Couldn't load world from file... {path}" );
		}
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
	public void Load( WorldSave save )
	{
		// Mark as current save
		CurrentSave = save.Id;
		WorldName = save.Name;

		// Host only
		if ( !Sandbox.Networking.IsHost )
		{
			Log.Info( "Tried to load a world save but we're not the host.." );
			return;
		}

		Log.Info( "Trying to load a world save..." );

		DestroyAnyTransientObjects();

		// Load the world from the save file
		LoadWorldState( save.WorldState );

		// Create GameObjects for every saved state GameObject
		foreach ( var obj in save.ObjectState.Objects )
		{
			var go = obj.CreateGameObject();
			go.NetworkSpawn();
		}
	}

	[Button( "Save World To File" )]
	public void SaveToFile()
	{
		if ( !FileSystem.Data.DirectoryExists( "worlds" ) )
			FileSystem.Data.CreateDirectory( "worlds" );

		var guid = CurrentSave;
		if ( guid == Guid.Empty ) guid = Guid.NewGuid();
		CurrentSave = guid;

		var save = PackWorld( WorldName );
		var fileName = $"worlds/{guid}.json";
		var json = Json.Serialize( save );

		FileSystem.Data.WriteAllText( fileName, json );
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

	public void LoadWorldState( VoxelWorldState state )
	{
		if ( state is null )
		{
			return;
		}

		if ( state.Version < 2 )
		{
			throw new NotImplementedException();
		}

		var world = GetVoxelWorld();
		var worldGen = world.Components.Get<VoxelWorldGen>();

		worldGen.Seed = state.Seed;
		worldGen.Parameters = ResourceLibrary.Get<WorldGenParameters>( state.ParametersPath );

		// TODO
		Assert.AreEqual( world.Size, state.Size );

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
		return (ushort) ((lower & 0x7f) | (upper << 7));
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

			return new ChunkState(
				new Vector3Int( chunk.ChunkPosX, chunk.ChunkPosY, chunk.ChunkPosZ ),
				Convert.ToBase64String( writer.ToArray() ) );
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

		var reader = ByteStream.CreateReader( Convert.FromBase64String( state.Data ) );

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
