﻿using Microsoft.AspNetCore.SignalR;
using webapi.Models;
using webapi.Repositories;
using webapi.Services;
using webapi.Services.Checkers;

namespace webapi.Hubs;

public class CheckersGameHub : Hub<ICheckersGameHub>
{
	private readonly CheckersGameService gameService;
	private readonly UsersRepository usersRepository;
	private readonly ILogger<CheckersLobbyHub> logger;

	public CheckersGameHub(CheckersGameService gameService, UsersRepository usersRepository, ILogger<CheckersLobbyHub> logger)
	{
		this.gameService = gameService;
		this.usersRepository = usersRepository;
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
			return Results.BadRequest("You don't have any active checkers game!");
		}

		game = gameService.TryMakeMove(game, user.PublicID, moves, out string error);
		if (game is null) return Results.BadRequest(error);

		await Clients.Group(game.Key).GameStateChanged();

		if (game.WinnerID is not null)
		{
			gameService.CloseGame(game);
			await gameService.AddGameToHistory(gameHistoryService, usersRepository, game);
		}

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
			return Results.BadRequest("You don't have any active checkers game!");
		}

		var gameState = gameService.GetRelativeGameState(user.PublicID);
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
