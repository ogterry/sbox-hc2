using System;
using System.Text.Json.Serialization;

namespace HC2;

/// <summary>
/// A character save.
/// </summary>
public sealed class CharacterSave
{
	/// <summary>
	/// What's our current save?
	/// </summary>
	[JsonIgnore]
	public static CharacterSave Current { get; set; }

	/// <summary>
	/// What's the id for this character?
	/// </summary>
	Guid Id { get; set; } = Guid.NewGuid();

	/// <summary>
	/// What's our character called?
	/// </summary>
	public string Name { get; set; } = "New Character";

	/// <summary>
	/// Extra save data that we'll serialize with the character.
	/// </summary>
	public string SaveData { get; set; } = null;

	/// <summary>
	/// Save this based on an existing player.
	/// </summary>
	/// <param name="player"></param>
	public void Save( Player player )
	{
		if ( !FileSystem.Data.DirectoryExists( "characters" ) )
			FileSystem.Data.CreateDirectory( "characters" );

		var saveData = new Dictionary<string, string>();
		foreach ( var component in player.GameObject.Components.GetAll<ISaveData>() )
		{
			var type = component.GetType().FullName;
			saveData[type] = component.Save();
		}

		// Try to save the current position so we can load that back in
		if ( WorldPersistence.Instance.CurrentSave != Guid.Empty )
		{
			saveData[$"location:{WorldPersistence.Instance.CurrentSave}"] = player.Transform.Position.ToString();
		}

		SaveData = Json.Serialize( saveData );

		FileSystem.Data.WriteJson( $"characters/{Id}.json", this );
	}

	/// <summary>
	/// Save the initial idea of a character, without a player reference.
	/// </summary>
	public void Save()
	{
		if ( !FileSystem.Data.DirectoryExists( "characters" ) )
			FileSystem.Data.CreateDirectory( "characters" );

		FileSystem.Data.WriteJson( $"characters/{Id}.json", this );
	}

	/// <summary>
	/// Deserialize all of our saved data into the player.
	/// </summary>
	/// <param name="player"></param>
	public void Load( Player player )
	{
		if ( string.IsNullOrEmpty( SaveData ) ) return;

		var data = Json.Deserialize<Dictionary<string, string>>( SaveData );

		foreach ( var component in player.Components.GetAll<ISaveData>() )
		{
			var type = component.GetType().FullName;
			if ( data.ContainsKey( type ) )
			{
				component.Load( data[type] );
			}
		}

		if ( data.TryGetValue( $"location:{WorldPersistence.Instance.CurrentSave}", out var positionAsString ) )
		{
			var position = Json.Deserialize<Vector3>( positionAsString );
			player.Transform.Position = position;
		}
	}

	public void Delete()
	{
		var file = $"characters/{Id}.json";

		if ( FileSystem.Data.FileExists( file ) )
			FileSystem.Data.DeleteFile( file );
	}

	public static List<CharacterSave> GetAll()
	{
		var files = FileSystem.Data.FindFile( "characters", "*" );
		var saves = new List<CharacterSave>();

		foreach ( var file in files )
		{
			var save = FileSystem.Data.ReadJson<CharacterSave>( "characters/" + file );
			if ( save is null ) 
				continue;

			save.Id = Guid.Parse( file.Replace( ".json", "" ) );
			saves.Add( save );
		}

		return saves;
	}
}
