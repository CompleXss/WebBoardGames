using Microsoft.AspNetCore.SignalR;
using webapi.Extensions;
using webapi.Models;
using webapi.Services;

namespace webapi.Hubs;

// TODO: Возможность перезагружать страницу и нормально переподключаться (+ задержка перед киком)
// TODO: Возможность кикать игроков

public class LobbyHub<TGame> : Hub<ILobbyHub> where TGame : PlayableGame
{
	private readonly ILobbyService lobbyService;
	private readonly IGameService gameService;
	private readonly ILogger<LobbyHub<TGame>> logger;

	public LobbyHub(ILobbyService lobbyService, IGameService gameService, ILogger<LobbyHub<TGame>> logger)
	{
		this.lobbyService = lobbyService;
		this.gameService = gameService;
		this.logger = logger;
	}



	public override async Task OnConnectedAsync()
	{
		var user = await GetUserInfoAsync();
		if (user is null)
		{
			logger.CouldNotGetUserInfoFromAccessToken();
			return;
		}

		// TODO: Возможность зайти в то же лобби с другого устройства?

		//var lobby = lobbyService.GetUserLobby(user.ID);

		//if (lobby is not null)
		//{
		//	await Clients.Caller.SendAsync(LOBBY_INFO, new { lobby, isHost = lobby.HostID == user.ID });
		//	return;
		//}

		//logger.UserConnectedToGameLobbyHub(user.Name, user.PublicID, GAME_NAME);
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		var user = await GetUserInfoAsync();
		if (user is null)
		{
			logger.CouldNotGetUserInfoFromAccessToken();
			return;
		}

		await lobbyService.LeaveLobby(user.PublicID, Context.ConnectionId);
		//logger.UserDisconnectedFromGameLobbyHub(user.Name, user.PublicID, GAME_NAME);
	}



	public async Task<IResult> CreateLobby()
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var lobby = await lobbyService.TryCreateLobbyAsync(user.PublicID, Context.ConnectionId);
		if (lobby is null)
			return Results.BadRequest("Could not create lobby");

		return Results.Ok(new { lobby });
	}

	// todo: new method. TEST
	public async Task<IResult> CloseLobby()
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var lobby = lobbyService.GetUserLobby(user.PublicID);
		if (lobby is null) return Results.BadRequest("You're not in a lobby.");
		if (lobby.HostID != user.PublicID) return Results.BadRequest("You're not a host of this lobby.");

		await lobbyService.LeaveLobby(user.PublicID, Context.ConnectionId);
		await lobbyService.CloseLobby(lobby, true);
		return Results.Ok();
	}



	public async Task<IResult> EnterLobby(string lobbyKey)
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var (lobby, errorResult) = await lobbyService.TryEnterLobbyAsync(user.PublicID, Context.ConnectionId, lobbyKey);
		if (lobby is null) return errorResult;

		logger.LogInformation("User with ID {userID} ENTERED lobby with key {lobbyKey}", user.PublicID, lobbyKey);
		return Results.Ok(new { lobby });
	}

	public async Task<IResult> LeaveLobby()
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var lobbyKey = await lobbyService.LeaveLobby(user.PublicID, Context.ConnectionId);

		logger.LogInformation("User with ID {userID} LEFT the lobby with key {lobbyKey}", user.PublicID, lobbyKey);
		return Results.Ok();
	}

	public async Task<IResult> StartGame()
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var lobby = lobbyService.GetUserLobby(user.PublicID);
		if (lobby is null) return Results.BadRequest("You're not in a lobby.");
		if (lobby.HostID != user.PublicID) return Results.BadRequest("You're not a host of this lobby.");
		if (!lobby.IsEnoughPlayersToStart) return Results.BadRequest("Not enough players to start the game.");

		bool gameStarted = gameService.TryStartNewGame(lobby.PlayerIDs, lobby.Settings);
		if (!gameStarted) return Results.BadRequest("Could not start the game.");

		await Clients.Group(lobby.Key).GameStarted();
		await lobbyService.CloseLobby(lobby, false);

		return Results.Ok();
	}



	private async Task<UserTokenInfo?> GetUserInfoAsync()
	{
		var httpContext = Context.GetHttpContext();
		if (httpContext is null) return null;

		return await AuthService.TryGetUserInfoFromHttpContextAsync(httpContext);
	}
}
