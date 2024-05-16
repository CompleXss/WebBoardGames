using Microsoft.AspNetCore.SignalR;
using webapi.Games;
using webapi.Models;
using webapi.Services;

namespace webapi.Hubs;

public class GameHub<TGame> : Hub<IGameHub> where TGame : PlayableGame
{
	private readonly IGameService gameService;
	private readonly ILogger<GameHub<TGame>> logger;

	public GameHub(IGameService gameService, ILogger<GameHub<TGame>> logger)
	{
		this.gameService = gameService;
		this.logger = logger;
	}



	public async override Task OnConnectedAsync()
	{
		if (Context.ConnectionId is null)
		{
			await Clients.Caller.NotAllowed();
			return;
		}

		var user = await GetUserInfoAsync();
		if (user is null)
		{
			logger.LogError("Can not get claims from Access Token!");
			return;
		}

		var game = gameService.GetUserGame(user.PublicID);
		if (game is null)
		{
			await Clients.Caller.NotAllowed();
			return;
		}

		if (game.WinnerID is not null)
		{
			gameService.CloseGame(game);
			await Clients.Caller.NotAllowed();
			return;
		}

		game.PlayersAlive++;
		await Clients.Group(game.Key).UserReconnected(user.PublicID);
		await Groups.AddToGroupAsync(Context.ConnectionId, game.Key);

		logger.LogInformation("User with ID {UserID} CONNECTED to checkers game hub.", user.PublicID);
	}

	public async override Task OnDisconnectedAsync(Exception? exception)
	{
		var user = await GetUserInfoAsync();
		if (user is null)
		{
			logger.LogError("Can not get claims from Access Token!");
			return;
		}

		var game = gameService.GetUserGame(user.PublicID);
		if (game is not null)
		{
			game.PlayersAlive--;
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, game.Key);

			logger.LogInformation("User with ID {UserID} DISCONNECTED from checkers game hub.", user.PublicID);

			if (game.PlayersAlive == 0)
				gameService.CloseGame(game);
			else
				await Clients.Group(game.Key).UserDisconnected(user.PublicID);
		}
	}



	public async Task<IResult> MakeMove(GameHistoryService gameHistoryService, CheckersMove[] moves)
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var game = gameService.GetUserGame(user.PublicID);
		if (game is null)
		{
			await Clients.Caller.NotAllowed();
			return Results.BadRequest("You don't have any active checkers game.");
		}

		if (!gameService.TryUpdateGameState(game, user.PublicID, moves, out string error))
			return Results.BadRequest(error);

		if (game.WinnerID is not null)
			await gameHistoryService.AddGameToHistoryAsync(game);

		// todo: close game

		await Clients.Group(game.Key).GameStateChanged();
		return Results.Ok();
	}

	public async Task<IResult> GetGameState()
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var game = gameService.GetUserGame(user.PublicID);
		if (game is null)
		{
			await Clients.Caller.NotAllowed();
			return Results.BadRequest("You don't have any active checkers game.");
		}

		var gameState = gameService.GetRelativeGameState(user.PublicID);
		if (gameState is null) return Results.Problem("Could not get game state.");

		return Results.Ok(gameState);
	}



	private async Task<UserTokenInfo?> GetUserInfoAsync()
	{
		var httpContext = Context.GetHttpContext();
		if (httpContext is null) return null;

		return await AuthService.TryGetUserInfoFromHttpContextAsync(httpContext);
	}
}
