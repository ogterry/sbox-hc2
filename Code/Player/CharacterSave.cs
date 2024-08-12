using System;

namespace HC2;

public enum CharacterType
{
	Class1,
	Class2,
	Class3
}

public sealed class CharacterSave
{
	public static CharacterSave Current { get; set; }

	Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; } = "New Character";
	public CharacterType CharacterType { get; set; } = CharacterType.Class1;
	public string SaveData { get; set; }

	public void Save( Player player, CharacterSave character )
	{
		if ( !FileSystem.Data.DirectoryExists( "characters" ) )
		{
			FileSystem.Data.CreateDirectory( "characters" );
		}

		var saveData = new Dictionary<Type, string>();
		foreach ( var component in player.Components.GetAll<ISaveData>() )
		{
			var type = component.GetType();
			saveData[type] = component.Save();
		}
		character.SaveData = Json.Serialize( saveData );

		FileSystem.Data.WriteJson( $"characters/{Id.ToString()}.json", this );
	}

	public void Save()
	{
		if ( !FileSystem.Data.DirectoryExists( "characters" ) )
		{
			FileSystem.Data.CreateDirectory( "characters" );
		}

		FileSystem.Data.WriteJson( $"characters/{Id.ToString()}.json", this );
	}

	public void Delete()
	{
		if ( FileSystem.Data.FileExists( $"characters/{Id.ToString()}.json" ) )
		{
			FileSystem.Data.DeleteFile( $"characters/{Id.ToString()}.json" );
		}
	}

	public static List<CharacterSave> GetAll()
	{
		var files = FileSystem.Data.FindFile( "characters", "*" );
		var saves = new List<CharacterSave>();
		foreach ( var file in files )
		{
			var save = FileSystem.Data.ReadJson<CharacterSave>( "characters/" + file );
			if ( save is null ) continue;
			save.Id = Guid.Parse( file.Replace( ".json", "" ) );
			saves.Add( save );
		}
		return saves;
	}
}
