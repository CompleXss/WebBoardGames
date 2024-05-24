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
			logger.LogError("Could not get claims from Access Token!");
			return;
		}

		var game = gameService.ConnectPlayer(user.PublicID, Context.ConnectionId);
		if (game is null)
		{
			await Clients.Caller.NotAllowed();
			return;
		}

		if (game.WinnerID is not null)
		{
			gameService.CloseGame(game.Key);
			await Clients.Caller.NotAllowed();
			return;
		}

		await Clients.Group(game.Key).UserReconnected(user.PublicID);
		await Groups.AddToGroupAsync(Context.ConnectionId, game.Key);

		logger.LogInformation("User with ID {userID} CONNECTED to {gameName} game hub.", user.PublicID, gameService.GameName);
	}

	public async override Task OnDisconnectedAsync(Exception? exception)
	{
		var user = await GetUserInfoAsync();
		if (user is null)
		{
			logger.LogError("Could not get claims from Access Token!");
			return;
		}

		var game = gameService.DisconnectPlayer(user.PublicID);
		if (game is null)
			return;

		await Groups.RemoveFromGroupAsync(Context.ConnectionId, game.Key);

		logger.LogInformation("User with ID {userID} DISCONNECTED from {gameName} game hub.", user.PublicID, gameService.GameName);

		if (game.PlayersConnected.Contains(true))
			await Clients.Group(game.Key).UserDisconnected(user.PublicID);
	}



	public async Task<IResult> MakeMove(object data)
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var game = gameService.GetUserGameInfo(user.PublicID);
		if (game is null)
		{
			await Clients.Caller.NotAllowed();
			return Results.BadRequest($"You don't have any active {gameService.GameName} game.");
		}

		if (!gameService.TryUpdateGameState(game.Key, user.PublicID, data, out string error))
			return Results.BadRequest(error);

		await Clients.Group(game.Key).GameStateChanged();
		return Results.Ok();
	}

	public async Task<IResult> Surrender()
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var game = gameService.GetUserGameInfo(user.PublicID);
		if (game is null)
		{
			await Clients.Caller.NotAllowed();
			return Results.BadRequest($"You don't have any active {gameService.GameName} game.");
		}

		return gameService.Surrender(user.PublicID)
			? Results.Ok()
			: Results.BadRequest("You can not surrender.");
	}

	public async Task<IResult> Request(object? data)
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var game = gameService.GetUserGameInfo(user.PublicID);
		if (game is null)
		{
			await Clients.Caller.NotAllowed();
			return Results.BadRequest($"You don't have any active {gameService.GameName} game.");
		}

		return gameService.Request(user.PublicID, data)
			? Results.Ok()
			: Results.BadRequest("You can not make this request.");
	}



	public async Task<IResult> SendChatMessage(string message)
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		var game = gameService.GetUserGameInfo(user.PublicID);
		if (game is null)
		{
			await Clients.Caller.NotAllowed();
			return Results.BadRequest($"You don't have any active {gameService.GameName} game.");
		}

		gameService.SendChatMessage(game.Key, message);
		return Results.Ok();
	}

	public async Task<IResult> GetGameState()
	{
		var user = await GetUserInfoAsync();
		if (user is null) return Results.Unauthorized();

		if (!gameService.IsUserInGame(user.PublicID))
		{
			await Clients.Caller.NotAllowed();
			return Results.BadRequest($"You don't have any active {gameService.GameName} game.");
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
