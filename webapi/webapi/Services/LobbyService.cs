﻿using Microsoft.AspNetCore.SignalR;
using webapi.Games;
using webapi.Hubs;
using webapi.Models;
using webapi.Types;

namespace webapi.Services;

public class LobbyService<TGame> : ILobbyService where TGame : PlayableGame
{
	private readonly LobbyCore lobbyCore;
	private readonly ConcurrentList<Lobby> lobbies = []; // order is not preserved

	private readonly IHubContext<LobbyHub<TGame>, ILobbyHub> hub;
	private readonly ILogger<LobbyService<TGame>> logger;

	public LobbyService(LobbyCore lobbyCore, IHubContext<LobbyHub<TGame>, ILobbyHub> hub, ILogger<LobbyService<TGame>> logger)
	{
		this.lobbyCore = lobbyCore;
		this.hub = hub;
		this.logger = logger;
	}



	private Lobby? GetLobbyByKey(string lobbyKey)
	{
		return lobbies.Find(x => x.Key == lobbyKey);
	}

	private Lobby? GetUserLobby(string userID)
	{
		return lobbies.Find(x => x.PlayerIDs.Contains(userID));
	}



	public LobbyInfo? GetUserLobbyInfo(string userID)
	{
		return GetUserLobby(userID)?.GetInfo();
	}

	public IEnumerable<LobbyInfo> GetAllPublicLobbies()
	{
		return lobbies.Where(x => x.IsPublic).Select(x => x.GetInfo());
	}

	public async Task<LobbyInfo?> TryCreateLobbyAsync(string hostID, string hostConnectionID)
	{
		if (GetUserLobby(hostID) is not null)
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

		logger.LogInformation("{gameName} lobby with key {lobbyKey} was CREATED", lobbyCore.GameName, lobby.Key);
		return lobby.GetInfo();
	}

	public async Task<(LobbyInfo? lobby, IResult errorResult)> TryEnterLobbyAsync(string userID, string connectionID, string lobbyKey)
	{
		if (GetUserLobby(userID) is not null)
			return (null, Results.BadRequest("Вы уже находитесь в лобби"));

		var lobby = GetLobbyByKey(lobbyKey);
		if (lobby is null)
			return (null, Results.NotFound($"Лобби с данным кодом ({lobbyKey}) не существует"));

		if (!lobby.TryAddPlayer(userID, connectionID))
			return (null, Results.BadRequest("В лобби не осталось мест"));

		await hub.Clients.Group(lobbyKey).UserConnected(userID);
		await hub.Groups.AddToGroupAsync(connectionID, lobbyKey);

		return (lobby.GetInfo(), Results.Empty);
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
		}
		catch (Exception) { }

		lobby.RemovePlayer(userID, connectionID);

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

	public bool MakeLobbyPublic(string lobbyKey, bool value)
	{
		var lobby = GetLobbyByKey(lobbyKey);
		if (lobby is null)
			return false;

		lobby.IsPublic = value;
		return true;
	}

	public bool SetLobbySettings(string lobbyKey, object? settings)
	{
		var lobby = GetLobbyByKey(lobbyKey);
		if (lobby is null)
			return false;

		lobby.Settings = settings;
		return true;
	}

	public bool SetLobbyHost(string lobbyKey, string userID)
	{
		var lobby = GetLobbyByKey(lobbyKey);
		if (lobby is null)
			return false;

		return lobby.TrySetHostID(userID);
	}



	public Task CloseLobby(string lobbyKey, bool notifyUsers)
	{
		var lobby = GetLobbyByKey(lobbyKey);
		if (lobby is null)
			return Task.CompletedTask;

		return CloseLobby(lobby, notifyUsers);
	}

	private async Task CloseLobby(Lobby lobby, bool notifyUsers)
	{
		if (!lobbies.Contains(lobby))
			return;

		lobby.MarkAsClosing();
		lobbies.RemoveBySwap(lobby);

		if (!lobby.IsEmpty)
		{
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

		try
		{
			await Task.WhenAll(tasks);
		}
		catch (Exception) { }
	}
}
