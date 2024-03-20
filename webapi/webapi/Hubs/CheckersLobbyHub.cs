using Microsoft.AspNetCore.SignalR;
using webapi.Models;
using webapi.Services;

namespace webapi.Hubs;

// TODO: Возможность перезагружать страницу и нормально переподключаться (+ задержка перед киком)
// TODO: Возможность кикать игроков

public class CheckersLobbyHub : Hub<ICheckersLobbyHub>
{
	private readonly CheckersLobbyService lobbyService;
	private readonly ILogger<CheckersLobbyHub> logger;

	public CheckersLobbyHub(CheckersLobbyService lobbyService, ILogger<CheckersLobbyHub> logger)
	{
		this.lobbyService = lobbyService;
		this.logger = logger;
	}



	public async override Task OnConnectedAsync()
	{
		var user = await GetUserInfoAsync();
		if (user is null)
		{
			logger.LogError("Can not get claims from Access Token!");
			return;
		}

		// TODO: Возможность зайти в то же лобби с другого устройства?

		//var lobby = lobbyService.GetUserLobby(user.ID);

		//if (lobby is not null)
		//{
		//	await Clients.Caller.SendAsync(LOBBY_INFO, new { lobby, isHost = lobby.HostID == user.ID });
		//	return;
		//}

		logger.LogInformation("User with ID {UserID} CONNECTED to checkers lobby hub.", user.ID);
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		var user = await GetUserInfoAsync();
		if (user is null)
		{
			logger.LogError("Can not get claims from Access Token!");
			return;
		}

		await lobbyService.LeaveLobby(user.ID, Context.ConnectionId);
		logger.LogInformation("User with ID {UserID} DISCONNECTED from checkers lobby hub.", user.ID);
	}



	public async Task<IResult> CreateLobby()
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var lobby = await lobbyService.TryCreateLobbyAsync(user.ID, Context.ConnectionId);
		if (lobby is null)
			return Results.BadRequest("Can not create lobby.");

		logger.LogInformation("Lobby with key {LobbyKey} was CREATED.", lobby.Key);
		return Results.Ok(new { lobby });
	}

	public async Task<IResult> EnterLobby(string lobbyKey)
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var (lobby, errorResult) = await lobbyService.TryEnterLobby(user.ID, Context.ConnectionId, lobbyKey);
		if (lobby is null) return errorResult;

		logger.LogInformation("User with ID {UserID} ENTERED lobby with key {LobbyKey}.", user.ID, lobbyKey);
		return Results.Ok(new { lobby });
	}

	public async Task<IResult> LeaveLobby()
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var lobbyKey = await lobbyService.LeaveLobby(user.ID, Context.ConnectionId);

		logger.LogInformation("User with ID {UserID} LEFT the lobby with key {LobbyKey}.", user.ID, lobbyKey);
		return Results.Ok();
	}

	public async Task<IResult> StartGame(CheckersGameService gameService)
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var lobby = lobbyService.GetUserLobby(user.ID);
		if (lobby is null) return Results.BadRequest("You're not in a lobby.");
		if (lobby.HostID != user.ID) return Results.BadRequest("You're not a host of this lobby.");
		if (!lobby.SecondPlayerID.HasValue) return Results.BadRequest("Lobby is not full.");

		gameService.CreateNewGame(lobby.HostID, lobby.SecondPlayerID.Value);

		await Clients.Group(lobby.Key).GameStarted();
		await lobbyService.RemoveAllUsersFromLobbyGroup(lobby);
		await lobbyService.CloseLobby(lobby);

		return Results.Ok();
	}



	private async Task<UserTokenInfo?> GetUserInfoAsync()
	{
		var httpContext = Context.GetHttpContext();
		if (httpContext is null) return null;

		return await AuthService.TryGetUserInfoFromHttpContextAsync(httpContext);
	}
}
