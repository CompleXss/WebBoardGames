using webapi.Hubs;

namespace webapi.Endpoints;

public static class LobbyEndpoints
{
	public static void MapLobbyEndpoints(this WebApplication app)
	{
		app.MapHub<CheckersLobbyHub>("/lobby/checkers");
	}
}
