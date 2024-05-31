using webapi.Games.Checkers;
using webapi.Games.Monopoly;
using webapi.Models;
using webapi.Services;

namespace webapi.Extensions;

public static class GameNameToLobbyServiceMapper
{
	public static ILobbyService? GetLobbyServiceForGame(this IServiceProvider serviceProvider, GameNames gameName)
	{
		return gameName switch
		{
			GameNames.checkers => serviceProvider.GetService<LobbyService<CheckersGame>>(),
			GameNames.monopoly => serviceProvider.GetService<LobbyService<MonopolyGame>>(),
			_ => null,
		};
	}
}
