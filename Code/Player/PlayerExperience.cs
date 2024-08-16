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
	[Property] public int MaxUpgrades { get; set; } = 10;

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

		using ( Rpc.FilterInclude( Network.OwnerConnection ) )
		{
			BroadcastMessage( $"+{amount} XP" );
		}

		if ( levels > 0 )
		{
			GiveLevel( levels );
		}
		else
		{
			CharacterSave.Current?.Save( Player.Local );
		}
	}

	[Authority]
	public void GiveLevel( int amount )
	{
		Level += amount;
		UnspentUpgrades += amount;
		OnGiveLevels?.Invoke( amount );

		using ( Rpc.FilterInclude( Network.OwnerConnection ) )
		{
			BroadcastMessage( $"You gained {amount} level(s)!" );
		}

		CharacterSave.Current?.Save( Player.Local );
	}

	public int GetPointsForLevel( int level )
	{
		return (int)XpCurve.Evaluate( (float)level / MaxLevel );
	}

	public void UpgradeStat( StatusEffect status, bool force = false )
	{
		if ( IsProxy ) return;
		if ( UnspentUpgrades <= 0 && !force ) return;
		if ( Upgrades.ContainsKey( status.ResourceName ) && Upgrades[status.ResourceName] >= MaxUpgrades ) return;

		var modifier = GameObject.Root.Components.Get<StatModifier>();
		if ( modifier == null ) return;

		if ( !Upgrades.ContainsKey( status.ResourceName ) )
			Upgrades[status.ResourceName] = 0;
		Upgrades[status.ResourceName]++;

		modifier.AddEffect( status );
		if ( !force )
		{
			UnspentUpgrades--;
			CharacterSave.Current?.Save( Player.Local );
		}
	}

	public void RemoveStat( StatusEffect status, bool force = false )
	{
		var modifier = Player.Local?.Components.Get<StatModifier>();
		if ( modifier == null ) return;

		modifier.RemoveEffect( status );
		if ( !force )
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
				RemoveStat( status, true );
		}
	}

	public string Save()
	{
		var upgradeString = "";
		foreach ( var upgrade in Upgrades )
		{
			upgradeString += $"{upgrade.Key}:{upgrade.Value},";
		}
		if ( upgradeString.EndsWith( "," ) ) upgradeString = upgradeString.Substring( 0, upgradeString.Length - 1 );
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
				UpgradeStat( status, true );
		}
	}

	[Broadcast( NetPermission.HostOnly )]
	void BroadcastMessage( string message )
	{
		NotificationPanel.Instance?.AddNotification( message );
	}
}
