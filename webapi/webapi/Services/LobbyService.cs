using Microsoft.AspNetCore.SignalR;
using webapi.Extensions;
using webapi.Hubs;
using webapi.Models;

namespace webapi.Services;

public class LobbyService<TGame> : ILobbyService where TGame : PlayableGame
{
	private readonly LobbyCore lobbyCore;
	private readonly List<Lobby> lobbies = []; // order is not preserved
	private readonly HashSet<string> usersInLobby = [];

	private readonly IHubContext<LobbyHub<TGame>, ILobbyHub> hub;
	private readonly ILogger<LobbyService<TGame>> logger;

	public LobbyService(LobbyCore lobbyCore, IHubContext<LobbyHub<TGame>, ILobbyHub> hub, ILogger<LobbyService<TGame>> logger)
	{
		this.lobbyCore = lobbyCore;
		this.hub = hub;
		this.logger = logger;
	}



	public Lobby? GetUserLobby(string userID)
	{
		return lobbies.FirstOrDefault(x => x.PlayerIDs.Contains(userID));
	}

	public async Task<Lobby?> TryCreateLobbyAsync(string hostID, string hostConnectionID)
	{
		if (!usersInLobby.Add(hostID))
			return null;

		var lobby = Lobby.TryCreateNew(lobbyCore, hostID, hostConnectionID);
		if (lobby is null)
			return null;

		lobby.HostChanged += hostID =>
		{
			hub.Clients.Groups(lobby.Key).HostChanged(hostID);
		};

		lobbies.Add(lobby);
		await hub.Groups.AddToGroupAsync(hostConnectionID, lobby.Key);

		logger.LogInformation("New {gameName} lobby with key {lobbyKey} was CREATED", lobbyCore.GameName, lobby.Key);
		return lobby;
	}

	public async Task<(Lobby? lobby, IResult errorResult)> TryEnterLobbyAsync(string userID, string connectionID, string lobbyKey)
	{
		if (usersInLobby.Contains(userID))
			return (null, Results.BadRequest("You are already in a lobby."));

		var lobby = lobbies.Find(x => x.Key == lobbyKey);
		if (lobby is null)
			return (null, Results.NotFound($"Lobby with the given key ({lobbyKey}) does not exist."));

		if (!lobby.TryAddPlayer(userID, connectionID))
			return (null, Results.BadRequest("Lobby is already full."));

		usersInLobby.Add(userID);
		await hub.Clients.Group(lobbyKey).UserConnected(userID);
		await hub.Groups.AddToGroupAsync(connectionID, lobbyKey);

		return (lobby, Results.Empty);
	}

	/// <summary> Disconnects user from the lobby. </summary>
	/// <returns> Key of the lobby which was left by user. Null if leave operation can not be done. </returns>
	public async Task<string?> LeaveLobby(string userID, string connectionID)
	{
		var lobby = GetUserLobby(userID);
		if (lobby is null) return null;

		try
		{
			await hub.Groups.RemoveFromGroupAsync(connectionID, lobby.Key);
			lobby.RemovePlayer(userID, connectionID);
		}
		catch (Exception) { }

		usersInLobby.Remove(userID);

		if (lobby.IsEmpty)
		{
			await CloseLobby(lobby, false);
		}
		else
		{
			await hub.Clients.Group(lobby.Key).UserDisconnected(userID);
		}

		return lobby.Key;
	}

	public async Task CloseLobby(Lobby lobby, bool notifyUsers)
	{
		if (!lobbies.Contains(lobby))
			return;

		lobbies.RemoveBySwap(lobby);

		if (!lobby.IsEmpty)
		{
			foreach (var playerID in lobby.PlayerIDs)
				usersInLobby.Remove(playerID);

			if (notifyUsers)
				await hub.Clients.Group(lobby.Key).LobbyClosed();

			await RemoveAllUsersFromLobbyGroup(lobby);
		}

		logger.LogInformation("{gameName} lobby with key {lobbyKey} was CLOSED", lobbyCore.GameName, lobby.Key);
		lobby.Dispose();
	}

	private async Task RemoveAllUsersFromLobbyGroup(Lobby lobby)
	{
		var tasks = new Task[lobby.ConnectionIDs.Count];

		for (int i = 0; i < lobby.ConnectionIDs.Count; i++)
			tasks[i] = hub.Groups.RemoveFromGroupAsync(lobby.ConnectionIDs[i], lobby.Key);

		await Task.WhenAll(tasks);
	}
}
