using webapi.Models;
using webapi.Models.Data;

namespace webapi.Extensions;

public static class Mapper
{
	public static GameHistoryDto ToDto(this GameHistory x)
	{
		return new GameHistoryDto
		{
			Winners = x.GamePlayers.Where(x => x.IsWinner).Select(x => x.User).ToArray(),
			Loosers = x.GamePlayers.Where(x => !x.IsWinner).Select(x => x.User).ToArray(),
			DateTimeStart = x.DateTimeStart.ToUniversalTime(),
			DateTimeEnd = x.DateTimeEnd.ToUniversalTime(),
		};
	}

	public static IEnumerable<GameHistoryDto> ToDto(this IEnumerable<GameHistory> history)
	{
		return history.Select(x => x.ToDto());
	}
}
