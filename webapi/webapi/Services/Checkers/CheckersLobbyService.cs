using Microsoft.AspNetCore.SignalR;
using webapi.Hubs;

namespace webapi.Services.Checkers;

public class CheckersLobbyService
{
	private readonly List<CheckersLobby> lobbies = new();
	private readonly HashSet<long> usersInLobby = new();

	private readonly IHubContext<CheckersLobbyHub, ICheckersLobbyHub> hub;
	private readonly ILogger<CheckersLobbyService> logger;

	public CheckersLobbyService(IHubContext<CheckersLobbyHub, ICheckersLobbyHub> hub, ILogger<CheckersLobbyService> logger)
	{
		this.hub = hub;
		this.logger = logger;
	}



	public CheckersLobby? GetUserLobby(long userID)
	{
		return lobbies.Find(x => x.HostID == userID || x.SecondPlayerID == userID);
	}

	public async Task<CheckersLobby?> TryCreateLobbyAsync(long hostID, string connectionID)
	{
		if (!usersInLobby.Add(hostID))
			return null;

		var lobby = new CheckersLobby(hostID);
		if (lobby.ErrorWhileCreating)
			return null;

		lobbies.Add(lobby);

		await hub.Groups.AddToGroupAsync(connectionID, lobby.Key);
		lobby.ConnectionIDs.Add(connectionID);

		logger.LogInformation("Lobby with key {LobbyKey} was CREATED.", lobby.Key);
		return lobby;
	}

	public async Task<(CheckersLobby? lobby, IResult errorResult)> TryEnterLobby(long userID, string connectionID, string lobbyKey)
	{
		if (usersInLobby.Contains(userID))
			return (null, Results.BadRequest("You are already in a lobby."));

		var lobby = lobbies.Find(x => x.Key == lobbyKey);
		if (lobby is null)
			return (null, Results.NotFound($"Lobby with the given key ({lobbyKey}) does not exists."));

		// If somehow user enters their own lobby, let it slip by
		if (lobby.HostID == userID)
			return (lobby, Results.Empty);

		if (lobby.SecondPlayerID.HasValue)
			return (null, Results.BadRequest("Lobby is already full."));

		usersInLobby.Add(userID);
		lobby.SecondPlayerID = userID;
		lobby.ConnectionIDs.Add(connectionID);

		await hub.Clients.Group(lobbyKey).UserConnected(userID);
		await hub.Groups.AddToGroupAsync(connectionID, lobbyKey);

		return (lobby, Results.Empty);
	}

	/// <summary> Disconnects user from the lobby. If user was the host, lobby closes. </summary>
	/// <returns> Key of the lobby which was left by user. Null if leave operation can not be done. </returns>
	public async Task<string?> LeaveLobby(long userID, string connectionID)
	{
		// return early if user is not in a lobby
		if (!usersInLobby.Remove(userID))
			return null;

		var lobby = lobbies.Find(x => x.HostID == userID || x.SecondPlayerID == userID);
		if (lobby is null) return null;

		await hub.Groups.RemoveFromGroupAsync(connectionID, lobby.Key);
		lobby.ConnectionIDs.Remove(connectionID);

		if (lobby.HostID == userID)
			await CloseLobby(lobby);
		else
		{
			lobby.SecondPlayerID = null;
			await hub.Clients.Group(lobby.Key).UserDisconnected(userID);
		}

		return lobby.Key;
	}

	public async Task CloseLobby(CheckersLobby lobby)
	{
		usersInLobby.Remove(lobby.HostID);
		if (lobby.SecondPlayerID.HasValue)
			usersInLobby.Remove(lobby.SecondPlayerID.Value);

		lobbies.Remove(lobby);

		await hub.Clients.Group(lobby.Key).LobbyClosed();
		await RemoveAllUsersFromLobbyGroup(lobby);

		logger.LogInformation("Lobby with key {LobbyKey} was CLOSED.", lobby.Key);
		lobby.Dispose();
	}

	public async Task RemoveAllUsersFromLobbyGroup(CheckersLobby lobby)
	{
		var tasks = new Task[lobby.ConnectionIDs.Count];

		for (int i = 0; i < lobby.ConnectionIDs.Count; i++)
			tasks[i] = hub.Groups.RemoveFromGroupAsync(lobby.ConnectionIDs[i], lobby.Key);

		await Task.WhenAll(tasks);
		lobby.ConnectionIDs.Clear();
	}
}
