namespace HC2;

public sealed class PlayerExperience : Component, ISaveData
{
	[Sync] public int Level { get; private set; } = 1;
	[Sync] public int Points { get; private set; } = 0;
	[Property] public Curve XpCurve { get; set; }
	[Property] public int MaxLevel { get; set; } = 40;

	public int MaxPoints => GetPointsForLevel( Level );

	[Authority]
	public void GivePoints( int amount )
	{
		Points += amount;

		while ( Points >= MaxPoints )
		{
			Points -= MaxPoints;
			GiveLevel( 1 );
		}
	}

	[Authority]
	public void GiveLevel( int amount )
	{
		Level += amount;
	}

	public int GetPointsForLevel( int level )
	{
		return (int)XpCurve.Evaluate( (float)level / MaxLevel );
	}

	public string Save()
	{
		return $"{Level}:{Points}";
	}

	public void Load( string data )
	{
		var parts = data.Split( ':' );
		if ( parts.Length != 2 )
			Log.Error( "Invalid save data found in PlayerExperience." );
		Level = int.Parse( parts[0] );
		Points = int.Parse( parts[1] );
	}
}
