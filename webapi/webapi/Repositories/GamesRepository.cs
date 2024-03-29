using Microsoft.EntityFrameworkCore;
using webapi.Data;

namespace webapi.Repositories;

public class GamesRepository(AppDbContext db)
{
	private readonly AppDbContext db = db;

	public Task<long> GetIdByName(string gameName)
	{
		return db.Games.Where(x => x.Name == gameName).Select(x => x.ID).FirstAsync();
	}
}
