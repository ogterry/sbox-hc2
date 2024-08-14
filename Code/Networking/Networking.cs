using Sandbox.Network;
using System;
using System.Threading.Tasks;
using HC2;

public sealed class Networking : Component, Component.INetworkListener
{
	public static Networking Instance { get; private set; }

	/// <summary>
	/// The prefab to spawn for the player to control.
	/// </summary>
	[Property]
	public GameObject PlayerPrefab { get; set; }

	protected override void OnAwake()
	{
		// So we can make Broadcast/Authority calls in this script
		if (!Network.Active)
		{
			GameObject.NetworkSpawn(null);
		}

		Instance = this;
	}

	protected override async Task OnLoad()
	{
		if (Scene.IsEditor)
			return;

		if (!GameNetworkSystem.IsActive)
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds(0.1f);
			GameNetworkSystem.CreateLobby();
		}
	}

	/// <summary>
	/// A client is fully connected to the server. This is called on the host.
	/// </summary>
	public void OnActive(Connection channel)
	{
		Log.Info($"Player '{channel.DisplayName}' has joined the game");

		using (Rpc.FilterInclude(channel))
		{
			SpawnLocalPlayer();
		}
	}

	[Broadcast]
	public async void SpawnLocalPlayer()
	{
		// Open Character Select Modal if we don't have a character selected
		if (CharacterSave.Current is null)
		{
			var modalObject = new GameObject();
			var screenPanel = modalObject.Components.Create<ScreenPanel>();
			screenPanel.ZIndex = 250;
			modalObject.Components.Create<CharacterSelectModal>();


			while (modalObject.IsValid())
			{
				await Task.Delay(100);
			}
		}

		SpawnPlayer();
	}

	[Authority]
	public void SpawnPlayer()
	{
		if (!Sandbox.Networking.IsHost) return;
		var channel = Rpc.Caller;

		if (Rpc.CallerId != channel.Id)
		{
			Log.Error("Player tried to spawn another player");
			return;
		}

		if (!PlayerPrefab.IsValid())
		{
			Log.Error("Player prefab is not valid");
			return;
		}

		// Find a spawn location for this player
		var startLocation = FindSpawnLocation().WithScale(1);

		// Spawn this object and make the client the owner
		var player = PlayerPrefab.Clone(startLocation, name: $"Player - {channel.DisplayName}");
		player.NetworkSpawn(channel);
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
		if (spawnPoints.Length > 0)
		{
			return Random.Shared.FromArray(spawnPoints).Transform.World;
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

		var orderedLobbies = lobbies.OrderByDescending(lobby => lobby.Members);

		foreach (var lobby in orderedLobbies)
		{
			if (lobby.IsFull) continue;

			Log.Info($"Joining lobby {lobby.LobbyId}");

			// Try to join this one
			if (await GameNetworkSystem.TryConnectSteamId(lobby.LobbyId))
				return true;
		}

		Log.Info($"Couldn't join a lobby - making a game");
		return false;
	}
}