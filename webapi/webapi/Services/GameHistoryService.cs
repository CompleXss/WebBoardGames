﻿using webapi.Models;
using webapi.Repositories;
using webapi.Data;
using webapi.Models.Data;
using System.Data;

namespace webapi.Services;

public class GameHistoryService
{
	private readonly AppDbContext db;
	private readonly GameHistoryRepository gameHistoryRepo;
	private readonly GamesRepository gamesRepo;
	private readonly UsersRepository usersRepo;

	public GameHistoryService(AppDbContext db, GameHistoryRepository gameHistoryRepo, GamesRepository gamesRepository, UsersRepository usersRepo)
	{
		this.db = db;
		this.gameHistoryRepo = gameHistoryRepo;
		this.gamesRepo = gamesRepository;
		this.usersRepo = usersRepo;
	}



	public async Task<bool> AddGameToHistoryAsync(PlayableGameInfo game)
	{
		if (game.WinnerID is null) return false;

		var winner = await usersRepo.GetByPublicIdAsync(game.WinnerID);
		var loosers = await Task.WhenAll(game.PlayerIDs
			.Where(x => x != game.WinnerID)
			.Select(async x => await usersRepo.GetByPublicIdAsync(x))
			.ToArray()
		);

		if (winner is null || loosers.Any(x => x is null))
			return false;

		var playHistoryDto = new GameHistoryDto()
		{
			Game = game.GameName,
			Winners = [winner],
			Loosers = loosers!,
			DateTimeStart = game.GameStarted,
			DateTimeEnd = DateTime.Now,
		};

		return await AddAsync(playHistoryDto);
	}

	public async Task<bool> AddAsync(GameHistoryDto history)
	{
		using var transaction = await db.Database.BeginTransactionAsync();
		string gameName = history.Game.ToString();

		try
		{
			// create history entry
			var gameID = await gamesRepo.GetIdByNameAsync(gameName);
			var historyEntry = new GameHistory()
			{
				GameID = gameID,
				DateTimeStart = history.DateTimeStart,
				DateTimeEnd = history.DateTimeEnd
			};
			await gameHistoryRepo.AddAsync(historyEntry);
			await db.SaveChangesAsync();



			// add players to history
			foreach (var winner in history.Winners)
			{
				await gameHistoryRepo.AddHistoryPlayerAsync(new GamePlayer
				{
					UserID = winner.ID,
					GameHistory = historyEntry,
					IsWinner = true,
				});
			}

			foreach (var looser in history.Loosers)
			{
				await gameHistoryRepo.AddHistoryPlayerAsync(new GamePlayer
				{
					UserID = looser.ID,
					GameHistory = historyEntry,
					IsWinner = false,
				});
			}

			await transaction.CommitAsync();
			return true;
		}
		catch (Exception)
		{
			await transaction.RollbackAsync();
			return false;
		}
	}
}
