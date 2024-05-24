using Microsoft.AspNetCore.SignalR;
using webapi.Hubs;
using webapi.Models;
using webapi.Services;
using webapi.Games;
using webapi.Games.Checkers;
using webapi.Games.Monopoly;

namespace webapi.Configuration;

public static class GameServicesConfigurator
{
	public static void AddGameServices(this IServiceCollection services)
	{
		// checkers
		services.AddServicesForGame<CheckersGame>(
			new LobbyCore(
				gameName: GameNames.checkers,
				maxLobbiesCount: 10_000,
				minPlayersToStartGame: 2,
				maxPlayersIncludingHost: 2
			),
			new GameCore(
				gameName: GameNames.checkers,
				maxGamesCount: 10_000,
				minPlayersCount: 2,
				maxPlayersCount: 2
			),
			(gameCore, hub, playerIDs, _) => new CheckersGame(gameCore, hub, playerIDs)
		);

		// monopoly
		services.AddServicesForGame<MonopolyGame>(
			new LobbyCore(
				gameName: GameNames.monopoly,
				maxLobbiesCount: 10_000,
				minPlayersToStartGame: 1,
				maxPlayersIncludingHost: 5
			),
			new GameCore(
				gameName: GameNames.monopoly,
				maxGamesCount: 10_000,
				minPlayersCount: 1,
				maxPlayersCount: 5
			),
			(gameCore, hub, playerIDs, _) => new MonopolyGame(gameCore, hub, playerIDs)
		);
	}

	private static void AddServicesForGame<TGame>(this IServiceCollection services, LobbyCore lobbyCore, GameCore gameCore, PlayableGame.Factory gameFactory)
		where TGame : PlayableGame
	{
		// register game service
		services.AddSingleton(serviceProvider =>
		{
			var hub = serviceProvider.GetRequiredService<IHubContext<GameHub<TGame>>>();
			var typedHub = serviceProvider.GetRequiredService<IHubContext<GameHub<TGame>, IGameHub>>();
			var logger = serviceProvider.GetRequiredService<ILogger<GameService<TGame>>>();

			return new GameService<TGame>(gameCore, gameFactory, hub, typedHub, serviceProvider, logger);
		});

		// register lobby service
		services.AddSingleton(serviceProvider =>
		{
			var hubContext = serviceProvider.GetRequiredService<IHubContext<LobbyHub<TGame>, ILobbyHub>>();
			var logger = serviceProvider.GetRequiredService<ILogger<LobbyService<TGame>>>();

			return new LobbyService<TGame>(lobbyCore, hubContext, logger);
		});

		// register game hub
		services.AddTransient(serviceProvider =>
		{
			var gameService = serviceProvider.GetRequiredService<GameService<TGame>>();
			var logger = serviceProvider.GetRequiredService<ILogger<GameHub<TGame>>>();

			return new GameHub<TGame>(gameService, logger);
		});

		// register lobby hub
		services.AddTransient(serviceProvider =>
		{
			var lobbyService = serviceProvider.GetRequiredService<LobbyService<TGame>>();
			var gameService = serviceProvider.GetRequiredService<GameService<TGame>>();
			var logger = serviceProvider.GetRequiredService<ILogger<LobbyHub<TGame>>>();

			return new LobbyHub<TGame>(lobbyService, gameService, logger);
		});
	}
}
