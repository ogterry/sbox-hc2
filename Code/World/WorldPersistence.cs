using Sandbox;
using System;
using System.Text.Json.Serialization;

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
public struct VoxelWorldState
{
	// stub
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
			var prefabFilePath = go.PrefabInstanceSource;

			Log.Info( go.PrefabInstanceSource );

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
	/// <summary>
	/// What's the target version?
	/// </summary>
	public const int CurrentVersion = 0;

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
	[HostSync] public string WorldName { get; set; } = "My World";
	[HostSync] public Guid CurrentSave { get; set; }

	public void LoadFromFile( string path, bool refreshSnapshot = true )
	{
		// TODO: load the world data file
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
		// Host only
		if ( !Sandbox.Networking.IsHost )
			return;

		DestroyAnyTransientObjects();

		// Load the world from the save file

		// Create GameObjects for every saved state GameObject
		foreach ( var obj in save.ObjectState.Objects )
		{
			obj.CreateGameObject();
		}

		// Send a full network snapshot
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

	/// <summary>
	/// Save the voxel world state into something that's readable 
	/// </summary>
	/// <returns></returns>
	public VoxelWorldState SaveWorldState()
	{
		return new()
		{
			// stub
		};
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
