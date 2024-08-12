using Sandbox.Network;
using System;
using System.Threading.Tasks;
using HC2;

public sealed class Networking : Component, Component.INetworkListener
{   
	/// <summary>
	/// The prefab to spawn for the player to control.
	/// </summary>
	[Property] 
	public GameObject PlayerPrefab { get; set; }

	protected override async Task OnLoad()
	{
		if ( Scene.IsEditor )
			return;

		if ( !GameNetworkSystem.IsActive )
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds( 0.1f );
			GameNetworkSystem.CreateLobby();
		}
	}

	/// <summary>
	/// A client is fully connected to the server. This is called on the host.
	/// </summary>
	public void OnActive( Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' has joined the game" );

		if ( !PlayerPrefab.IsValid() )
			return;

		//
		// Find a spawn location for this player
		//
		var startLocation = FindSpawnLocation().WithScale( 1 );

		// Spawn this object and make the client the owner
		var player = PlayerPrefab.Clone( startLocation, name: $"Player - {channel.DisplayName}" );
		var playerComponent = player.Components.Get<Player>();
		playerComponent.Inventory = Inventory.Create( 8, channel );
		player.NetworkSpawn( channel );
	}

	/// <summary>
	/// Find the most appropriate place to respawn
	/// </summary>
	Transform FindSpawnLocation()
	{
		//
		// If we have any SpawnPoint components in the scene, then use those
		//
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		if ( spawnPoints.Length > 0 )
		{
			return Random.Shared.FromArray( spawnPoints ).Transform.World;
		}

		//
		// Failing that, spawn where we are
		//
		return Transform.World;
	}

	/// <summary>
	/// Try to join any lobby for this game
	/// </summary>
	public static async Task<bool> TryJoinLobby()
	{
		var lobbies = await Sandbox.Networking.QueryLobbies();

		var orderedLobbies = lobbies.OrderByDescending( lobby => lobby.Members );

		foreach ( var lobby in orderedLobbies )
		{
			if ( lobby.IsFull ) continue;

			Log.Info( $"Joining lobby {lobby.LobbyId}" );

			// Try to join this one
			if ( await GameNetworkSystem.TryConnectSteamId( lobby.LobbyId ) )
				return true;
		}

		Log.Info( $"Couldn't join a lobby - making a game" );
		return false;
	}
}
