using webapi.Models.GameModels;

namespace webapi.Services;

public class CheckersService
{
	private readonly List<CheckersGame> activeGames = new();

	private readonly List<CheckersLobbyRoom> lobby = new();

	public string GetRoomKey(long hostID)
	{
		var room = lobby.FirstOrDefault(x => x.HostID == hostID);
		if (room is null)
		{
			room = new CheckersLobbyRoom(hostID);
			lobby.Add(room);
		}

		return room.RoomKey;
	}

	public void SetLobbyHostIsAlive(long hostID)
	{

	}
}
