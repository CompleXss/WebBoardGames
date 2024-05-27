using Microsoft.AspNetCore.SignalR;
using webapi.Extensions;
using webapi.Games;
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

		var existingLobby = lobbyService.GetUserLobbyInfo(user.PublicID);
		if (existingLobby is not null)
			return Results.BadRequest("Вы уже находитесь в этом лобби");

		var lobby = await lobbyService.TryCreateLobbyAsync(user.PublicID, Context.ConnectionId);
		if (lobby is null)
			return Results.BadRequest("Не получилось создать лобби");

		return Results.Ok(new { lobby });
	}

	// todo: new method. TEST
	public async Task<IResult> CloseLobby()
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var lobby = lobbyService.GetUserLobbyInfo(user.PublicID);
		if (lobby is null) return Results.BadRequest("Ошибка. Лобби не найдено");
		if (lobby.HostID != user.PublicID) return Results.BadRequest("Вы не хост этого лобби");

		await lobbyService.LeaveLobby(user.PublicID, Context.ConnectionId);
		await lobbyService.CloseLobby(lobby.Key, true);
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

		var lobby = lobbyService.GetUserLobbyInfo(user.PublicID);
		if (lobby is null) return Results.BadRequest("Ошибка. Лобби не найдено");
		if (lobby.HostID != user.PublicID) return Results.BadRequest("Вы не хост этого лобби");
		if (!lobby.IsEnoughPlayersToStart) return Results.BadRequest("Недостаточно игроков для начала");

		bool gameStarted = gameService.TryStartNewGame(lobby.PlayerIDs, lobby.Settings);
		if (!gameStarted) return Results.BadRequest("Не получилось запустить игру");

		await Clients.Group(lobby.Key).GameStarted();
		await lobbyService.CloseLobby(lobby.Key, false);

		return Results.Ok();
	}



	private async Task<UserTokenInfo?> GetUserInfoAsync()
	{
		var httpContext = Context.GetHttpContext();
		if (httpContext is null) return null;

		return await AuthService.TryGetUserInfoFromHttpContextAsync(httpContext);
	}
}
