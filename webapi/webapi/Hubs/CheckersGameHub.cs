using Microsoft.AspNetCore.SignalR;
using webapi.Models;
using webapi.Models.GameModels.Checkers;
using webapi.Services;

namespace webapi.Hubs;

public class CheckersGameHub : Hub
{
	public const string NOT_ALLOWED = "NotAllowed";
	public const string GAME_STATE_CHANGED = "GameStateChanged";
	public const string USER_DISCONNECTED = "UserDisconnected";
	public const string USER_RECONNECTED = "UserReconnected";

	private readonly CheckersGameService gameService;
	private readonly ILogger<CheckersLobbyHub> logger;

	public CheckersGameHub(CheckersGameService gameService, ILogger<CheckersLobbyHub> logger)
	{
		this.gameService = gameService;
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

		var game = gameService.GetUserGame(user.ID);
		if (game is null)
		{
			await Clients.Caller.SendAsync(NOT_ALLOWED);
			return;
		}

		game.PlayersAlive++;
		game.ConnectionIDs.Add(Context.ConnectionId);
		await Clients.Group(game.Key).SendAsync(USER_RECONNECTED, user.ID);
		await Groups.AddToGroupAsync(Context.ConnectionId, game.Key);

		logger.LogInformation("User with ID {UserID} CONNECTED to checkers game hub.", user.ID);
	}

	public async override Task OnDisconnectedAsync(Exception? exception)
	{
		var user = await GetUserInfoAsync();
		if (user is null)
		{
			logger.LogError("Can not get claims from Access Token!");
			return;
		}

		var game = gameService.GetUserGame(user.ID);
		if (game is not null)
		{
			game.PlayersAlive--;
			game.ConnectionIDs.Remove(Context.ConnectionId);
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, game.Key);

			logger.LogInformation("User with ID {UserID} DISCONNECTED from checkers game hub.", user.ID);

			if (game.PlayersAlive == 0)
				gameService.CloseGame(game);
			else
				await Clients.Group(game.Key).SendAsync(USER_DISCONNECTED, user.ID);
		}
	}



	public async Task<IResult> MakeMove(GameHistoryService gameHistoryService, CheckersMove[] moves)
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var game = gameService.GetUserGame(user.ID);
		if (game is null)
		{
			await Clients.Caller.SendAsync(NOT_ALLOWED);
			return Results.BadRequest("У тебя нет активной игры в шашки!");
		}

		game = gameService.TryMakeMove(game, user.ID, moves, out string error);
		if (game is null) return Results.BadRequest(error);

		if (game.WinnerID.HasValue)
			await gameService.AddGameToHistory(gameHistoryService, game);

		await Clients.Group(game.Key).SendAsync(GAME_STATE_CHANGED);
		return Results.Ok();
	}

	public async Task<IResult> GetGameState()
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var game = gameService.GetUserGame(user.ID);
		if (game is null)
		{
			await Clients.Caller.SendAsync(NOT_ALLOWED);
			return Results.BadRequest("У тебя нет активной игры в шашки!");
		}

		var gameState = gameService.GetRelativeGameState(user.ID);
		if (gameState is null) return Results.Problem("Не удалось получить состояние поля.");

		return Results.Ok(gameState);
	}



	private async Task<UserTokenInfo?> GetUserInfoAsync()
	{
		var httpContext = Context.GetHttpContext();
		if (httpContext is null) return null;

		return await AuthService.TryGetUserInfoFromHttpContextAsync(httpContext);
	}
}
