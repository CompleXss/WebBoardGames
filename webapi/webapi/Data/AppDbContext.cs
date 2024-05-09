using Microsoft.EntityFrameworkCore;
using webapi.Extensions;
using webapi.Models;
using webapi.Models.Data;

namespace webapi.Data;

public partial class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	{
	}

	/// <summary> Ensures that the database contains all necessary default values. </summary>
	/// <returns> The task result containing bad entries count in database. </returns>
	public async Task<int> CreateMissingDefaultData(ILogger logger)
	{
		int badEntriesCount = 0;

		// games table
		foreach (var gameName in Enum.GetNames<Games>())
			if (!await Games.AnyAsync(x => x.Name == gameName))
			{
				// if gameName is present but in different case
				var invalidDbEntry = await Games.FirstOrDefaultAsync(x => x.Name.ToLower() == gameName.ToLower()); // StringComparison doesn't work
				if (invalidDbEntry is not null)
				{
					badEntriesCount++;
					logger.DatabaseHasDefaultEntryInDifferentCase(nameof(Games), invalidDbEntry.Name, gameName);
					continue;
				}

				await Games.AddAsync(new Game()
				{
					Name = gameName,
				});
				logger.CreatedNewDefaultEntry(nameof(Games), gameName);

				await SaveChangesAsync();
			}

		return badEntriesCount;
	}

	public virtual DbSet<Game> Games { get; set; }

	public virtual DbSet<GameHistory> GameHistories { get; set; }

	public virtual DbSet<GamePlayer> GamePlayers { get; set; }

	public virtual DbSet<User> Users { get; set; }

	public virtual DbSet<UserGameStatistic> UserGameStatistics { get; set; }

	public virtual DbSet<UserRefreshToken> UserRefreshTokens { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Game>(entity =>
		{
			entity.HasKey(e => e.ID).HasName("games_pkey");

			entity.ToTable("games");

			entity.HasIndex(e => e.Name, "games_name_key").IsUnique();

			entity.Property(e => e.ID)
				.UseIdentityAlwaysColumn()
				.HasColumnName("id");
			entity.Property(e => e.Name)
				.HasColumnType("character varying")
				.HasColumnName("name");
		});

		modelBuilder.Entity<GameHistory>(entity =>
		{
			entity.HasKey(e => e.ID).HasName("game_history_pkey");

			entity.ToTable("game_history");

			entity.Property(e => e.ID)
				.UseIdentityAlwaysColumn()
				.HasColumnName("id");
			entity.Property(e => e.DateTimeEnd)
				.HasConversion(x => x.ToUniversalTime(), x => x.ToLocalTime())
				.HasPrecision(0)
				.HasColumnName("date_time_end");
			entity.Property(e => e.DateTimeStart)
				.HasConversion(x => x.ToUniversalTime(), x => x.ToLocalTime())
				.HasPrecision(0)
				.HasColumnName("date_time_start");
			entity.Property(e => e.GameID).HasColumnName("game_id");

			entity.HasOne(d => d.Game).WithMany(p => p.GameHistories)
				.HasForeignKey(d => d.GameID)
				.HasConstraintName("game_history_game_id_fkey");
		});

		modelBuilder.Entity<GamePlayer>(entity =>
		{
			entity.HasKey(e => new { e.GameHistoryID, e.UserID }).HasName("game_players_pkey");

			entity.ToTable("game_players");

			entity.Property(e => e.GameHistoryID).HasColumnName("game_history_id");
			entity.Property(e => e.UserID).HasColumnName("user_id");
			entity.Property(e => e.IsWinner).HasColumnName("is_winner");

			entity.HasOne(d => d.GameHistory).WithMany(p => p.GamePlayers)
				.HasForeignKey(d => d.GameHistoryID)
				.HasConstraintName("game_players_game_history_id_fkey");

			entity.HasOne(d => d.User).WithMany(p => p.GamePlayers)
				.HasForeignKey(d => d.UserID)
				.OnDelete(DeleteBehavior.SetNull)
				.HasConstraintName("game_players_user_id_fkey");
		});

		modelBuilder.Entity<User>(entity =>
		{
			entity.HasKey(e => e.ID).HasName("users_pkey");

			entity.ToTable("users");

			entity.HasIndex(e => e.Login, "users_login_key").IsUnique();

			entity.HasIndex(e => e.PublicID, "users_public_id_key").IsUnique();

			entity.Property(e => e.ID)
				.UseIdentityAlwaysColumn()
				.HasColumnName("id");
			entity.Property(e => e.Login)
				.HasColumnType("character varying")
				.HasColumnName("login");
			entity.Property(e => e.Name)
				.HasColumnType("character varying")
				.HasColumnName("name");
			entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
			entity.Property(e => e.PasswordSalt).HasColumnName("password_salt");
			entity.Property(e => e.PublicID)
				.HasMaxLength(32)
				.IsFixedLength()
				.HasColumnName("public_id");
		});

		modelBuilder.Entity<UserGameStatistic>(entity =>
		{
			entity.HasKey(e => new { e.UserID, e.GameID }).HasName("user_game_statistics_pkey");

			entity.ToTable("user_game_statistics");

			entity.Property(e => e.UserID).HasColumnName("user_id");
			entity.Property(e => e.GameID).HasColumnName("game_id");
			entity.Property(e => e.PlayCount)
				.HasDefaultValue(0)
				.HasColumnName("play_count");
			entity.Property(e => e.WinCount)
				.HasDefaultValue(0)
				.HasColumnName("win_count");

			entity.HasOne(d => d.Game).WithMany(p => p.UserGameStatistics)
				.HasForeignKey(d => d.GameID)
				.HasConstraintName("user_game_statistics_game_id_fkey");

			entity.HasOne(d => d.User).WithMany(p => p.UserGameStatistics)
				.HasForeignKey(d => d.UserID)
				.HasConstraintName("user_game_statistics_user_id_fkey");
		});

		modelBuilder.Entity<UserRefreshToken>(entity =>
		{
			entity.HasKey(e => new { e.UserID, e.DeviceID }).HasName("user_refresh_token_pkey");

			entity.ToTable("user_refresh_token");

			entity.Property(e => e.UserID).HasColumnName("user_id");
			entity.Property(e => e.DeviceID)
				.HasMaxLength(32)
				.IsFixedLength()
				.HasColumnName("device_id");
			entity.Property(e => e.RefreshTokenHash).HasColumnName("refresh_token_hash");
			entity.Property(e => e.TokenCreated)
				.HasConversion(x => x.ToUniversalTime(), x => x.ToLocalTime())
				.HasPrecision(0)
				.HasColumnName("token_created");
			entity.Property(e => e.TokenExpires)
				.HasConversion(x => x.ToUniversalTime(), x => x.ToLocalTime())
				.HasPrecision(0)
				.HasColumnName("token_expires");

			entity.HasOne(d => d.User).WithMany(p => p.UserRefreshTokens)
				.HasForeignKey(d => d.UserID)
				.HasConstraintName("user_refresh_token_user_id_fkey");
		});

		OnModelCreatingPartial(modelBuilder);
	}

	partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
