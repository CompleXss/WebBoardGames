using Microsoft.AspNetCore.SignalR;
using webapi.Models;
using webapi.Services;
using webapi.Services.Checkers;

namespace webapi.Hubs;

public class CheckersGameHub : Hub<ICheckersGameHub>
{
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
			await Clients.Caller.NotAllowed();
			return;
		}

		game.PlayersAlive++;
		game.ConnectionIDs.Add(Context.ConnectionId);
		await Clients.Group(game.Key).UserReconnected(user.ID);
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
				await Clients.Group(game.Key).UserDisconnected(user.ID);
		}
	}



	public async Task<IResult> MakeMove(GameHistoryService gameHistoryService, CheckersMove[] moves)
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var game = gameService.GetUserGame(user.ID);
		if (game is null)
		{
			await Clients.Caller.NotAllowed();
			return Results.BadRequest("You don't have any active checkers game!");
		}

		game = gameService.TryMakeMove(game, user.ID, moves, out string error);
		if (game is null) return Results.BadRequest(error);

		if (game.WinnerID.HasValue)
			await gameService.AddGameToHistory(gameHistoryService, game);

		await Clients.Group(game.Key).GameStateChanged();
		return Results.Ok();
	}

	public async Task<IResult> GetGameState()
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var game = gameService.GetUserGame(user.ID);
		if (game is null)
		{
			await Clients.Caller.NotAllowed();
			return Results.BadRequest("You don't have any active checkers game!");
		}

		var gameState = gameService.GetRelativeGameState(user.ID);
		if (gameState is null) return Results.Problem("Can not get field state.");

		return Results.Ok(gameState);
	}



	private async Task<UserTokenInfo?> GetUserInfoAsync()
	{
		var httpContext = Context.GetHttpContext();
		if (httpContext is null) return null;

		return await AuthService.TryGetUserInfoFromHttpContextAsync(httpContext);
	}
}
