using Microsoft.AspNetCore.SignalR;
using webapi.Endpoints;
using webapi.Models;
using webapi.Services;

namespace webapi.Hubs;

public class CheckersLobbyHub : Hub
{
	public const string USER_CONNECTED = "UserConnected";
	public const string USER_DISCONNECTED = "UserDisconnected";
	public const string LOBBY_INFO = "LobbyInfo";
	public const string LOBBY_CLOSED = "LobbyClosed";

	private readonly CheckersLobbyService lobbyService;
	private readonly ILogger<CheckersLobbyHub> logger;

	public CheckersLobbyHub(CheckersLobbyService lobbyService, ILogger<CheckersLobbyHub> logger)
	{
		this.lobbyService = lobbyService;
		this.logger = logger;
	}



	public override Task OnConnectedAsync()
	{
		var user = GetUserInfo();
		if (user is null)
		{
			logger.LogError("Can not get claims from Access Token!");
			return Task.CompletedTask;
		}

		// TODO: Возможность зайти в то же лобби с другого устройства?

		//var lobby = lobbyService.GetUserLobby(user.ID);

		//if (lobby is not null)
		//{
		//	await Clients.Caller.SendAsync(LOBBY_INFO, new { lobby, isHost = lobby.HostID == user.ID });
		//	return;
		//}

		logger.LogInformation("User with ID {UserID} CONNECTED to checkers hub.", user.ID);
		return Task.CompletedTask;
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		var user = GetUserInfo();
		if (user is null)
		{
			logger.LogError("Can not get claims from Access Token!");
			return;
		}

		await lobbyService.LeaveLobby(user.ID, Context.ConnectionId);
		logger.LogInformation("User with ID {UserID} DISCONNECTED from checkers hub.", user.ID);
	}



	public async Task<IResult> CreateLobby(ILogger<CheckersLobbyHub> logger)
	{
		var user = GetUserInfo();
		if (user is null) return Results.Unauthorized();

		var lobby = await lobbyService.TryCreateLobbyAsync(user.ID, Context.ConnectionId);
		if (lobby is null)
			return Results.BadRequest("You are already in a lobby.");

		logger.LogInformation("Lobby with key {LobbyKey} was CREATED.", lobby.Key);
		return Results.Ok(new { lobby });
	}

	public async Task<IResult> EnterLobby(string lobbyKey)
	{
		var user = GetUserInfo();
		if (user is null) return Results.Unauthorized();

		var (lobby, errorResult) = await lobbyService.TryEnterLobby(user.ID, Context.ConnectionId, lobbyKey);
		if (lobby is null) return errorResult;

		logger.LogInformation("User with ID {UserID} ENTERED lobby with key {LobbyKey}.", user.ID, lobbyKey);
		return Results.Ok(new { lobby });
	}

	public async Task<IResult> LeaveLobby()
	{
		var user = GetUserInfo();
		if (user is null) return Results.Unauthorized();

		var lobbyKey = await lobbyService.LeaveLobby(user.ID, Context.ConnectionId);

		logger.LogInformation("User with ID {UserID} LEFT the lobby with key {LobbyKey}.", user.ID, lobbyKey);
		return Results.Ok();
	}



	private UserTokenInfo? GetUserInfo()
	{
		string? accessToken = GetAccessToken();
		if (accessToken is null)
			return null;

		return AuthService.GetUserInfoFromAccessToken(accessToken);
	}

	private string? GetAccessToken()
	{
		var httpContext = Context.GetHttpContext();
		return httpContext?.Request.Cookies[AuthEndpoint.ACCESS_TOKEN_COOKIE_NAME];
	}
}
