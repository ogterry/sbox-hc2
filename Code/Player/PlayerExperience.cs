using System;

namespace HC2;

public sealed class PlayerExperience : Component, ISaveData
{
	[Sync] public int Level { get; private set; } = 1;
	[Sync] public int Points { get; private set; } = 0;
	public int UnspentUpgrades { get; private set; } = 0;
	public Dictionary<string, int> Upgrades { get; private set; } = new();

	[Property] public Curve XpCurve { get; set; }
	[Property] public int MaxLevel { get; set; } = 40;

	public Action<int> OnGivePoints { get; set; }
	public Action<int> OnGiveLevels { get; set; }

	public int MaxPoints => GetPointsForLevel( Level );

	[Authority]
	public void GivePoints( int amount )
	{
		Points += amount;
		OnGivePoints?.Invoke( amount );

		int levels = 0;
		while ( Points >= MaxPoints )
		{
			Points -= MaxPoints;
			levels++;
		}

		if ( levels > 0 )
		{
			GiveLevel( levels );
		}
	}

	[Authority]
	public void GiveLevel( int amount )
	{
		Level += amount;
		UnspentUpgrades += amount;
		OnGiveLevels?.Invoke( amount );
	}

	public int GetPointsForLevel( int level )
	{
		return (int)XpCurve.Evaluate( (float)level / MaxLevel );
	}

	public void UpgradeStat( StatusEffect status )
	{
		if ( UnspentUpgrades <= 0 ) return;

		var modifier = Player.Local?.Components.Get<StatModifier>();
		if ( modifier == null ) return;

		modifier.AddEffect( status );
		UnspentUpgrades--;
	}

	public void RemoveStat( StatusEffect status )
	{
		var modifier = Player.Local?.Components.Get<StatModifier>();
		if ( modifier == null ) return;

		modifier.RemoveEffect( status );
		UnspentUpgrades++;
	}

	void ClearStats()
	{
		var statusList = ResourceLibrary.GetAll<StatusEffect>();

		foreach ( var upgrade in Upgrades )
		{
			var status = statusList.FirstOrDefault( x => x.ResourceName == upgrade.Key );
			if ( status == null ) continue;

			for ( int i = 0; i < upgrade.Value; i++ )
				RemoveStat( status );
		}
	}

	public string Save()
	{
		var upgradeString = "";
		foreach ( var upgrade in Upgrades )
		{
			upgradeString += $"{upgrade.Key}:{upgrade.Value},";
		}
		return $"{Level};{Points};{UnspentUpgrades};{upgradeString}";
	}

	public void Load( string data )
	{
		var parts = data.Split( ';' );
		if ( parts.Length != 4 )
			Log.Error( "Invalid save data found in PlayerExperience." );
		Level = int.Parse( parts[0] );
		Points = int.Parse( parts[1] );
		UnspentUpgrades = int.Parse( parts[2] );
		var upgradeString = parts[3].Split( ',' );

		ClearStats();
		var statusList = ResourceLibrary.GetAll<StatusEffect>();
		foreach ( var upgrade in upgradeString )
		{
			if ( string.IsNullOrEmpty( upgrade ) ) continue;
			var upgradeParts = upgrade.Split( ':' );
			if ( upgradeParts.Length != 2 ) continue;

			var upgradeName = upgradeParts[0];
			var upgradeCount = int.Parse( upgradeParts[1] );
			var status = statusList.FirstOrDefault( x => x.ResourceName == upgradeName );
			if ( status == null ) continue;

			for ( int i = 0; i < upgradeCount; i++ )
				UpgradeStat( status );
		}
	}
}
