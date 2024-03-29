using Microsoft.EntityFrameworkCore;
using webapi.Models;

namespace webapi.Data;

public partial class AppDbContext : DbContext
{
	private const string DATETIME_STRING_FORMAT = "yyyy-MM-dd HH:mm:ss";

	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	{
		Database.EnsureCreated();
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
			entity.HasIndex(e => e.Name, "IX_Games_Name").IsUnique();

			entity.Property(e => e.ID).HasColumnName("ID");
		});

		modelBuilder.Entity<GameHistory>(entity =>
		{
			entity.ToTable("Game_History");

			entity.Property(e => e.ID).HasColumnName("ID");
			entity.Property(e => e.GameID).HasColumnName("GameID");

			entity.Property(e => e.DateTimeStart).HasConversion(x => x.ToString(DATETIME_STRING_FORMAT), x => DateTime.Parse(x));
			entity.Property(e => e.DateTimeEnd).HasConversion(x => x.ToString(DATETIME_STRING_FORMAT), x => DateTime.Parse(x));

			entity.HasOne(d => d.Game).WithMany(p => p.GameHistories)
				.HasForeignKey(d => d.GameID)
				.OnDelete(DeleteBehavior.ClientSetNull);
		});

		modelBuilder.Entity<GamePlayer>(entity =>
		{
			entity.HasKey(e => new { e.UserID, e.GameHistoryID });

			entity.ToTable("Game_Players");

			entity.Property(e => e.UserID).HasColumnName("UserID");
			entity.Property(e => e.GameHistoryID).HasColumnName("GameHistoryID");

			entity.HasOne(d => d.GameHistory).WithMany(p => p.GamePlayers).HasForeignKey(d => d.GameHistoryID);

			entity.HasOne(d => d.User).WithMany(p => p.GamePlayers)
				.HasForeignKey(d => d.UserID)
				.OnDelete(DeleteBehavior.SetNull);
		});

		modelBuilder.Entity<User>(entity =>
		{
			entity.HasIndex(e => e.Login, "IX_Users_Login").IsUnique();

			entity.HasIndex(e => e.PublicID, "IX_Users_PublicID").IsUnique();

			entity.Property(e => e.ID).HasColumnName("ID");
			entity.Property(e => e.PublicID).HasColumnName("PublicID");
		});

		modelBuilder.Entity<UserGameStatistic>(entity =>
		{
			entity.HasKey(e => new { e.UserID, e.GameID });

			entity.ToTable("User_GameStatistics");

			entity.Property(e => e.UserID).HasColumnName("UserID");
			entity.Property(e => e.GameID).HasColumnName("GameID");

			entity.HasOne(d => d.Game).WithMany(p => p.UserGameStatistics)
				.HasForeignKey(d => d.GameID)
				.OnDelete(DeleteBehavior.ClientSetNull);

			entity.HasOne(d => d.User).WithMany(p => p.UserGameStatistics).HasForeignKey(d => d.UserID);
		});

		modelBuilder.Entity<UserRefreshToken>(entity =>
		{
			entity.HasKey(e => new { e.UserID, e.DeviceID });

			entity.ToTable("User_RefreshToken");

			entity.Property(e => e.UserID).HasColumnName("UserID");
			entity.Property(e => e.DeviceID).HasColumnName("DeviceID");

			entity.Property(e => e.TokenCreated).HasConversion(x => x.ToString(DATETIME_STRING_FORMAT), x => DateTime.Parse(x));
			entity.Property(e => e.TokenExpires).HasConversion(x => x.ToString(DATETIME_STRING_FORMAT), x => DateTime.Parse(x));

			entity.HasOne(d => d.User).WithMany(p => p.UserRefreshTokens).HasForeignKey(d => d.UserID);
		});

		OnModelCreatingPartial(modelBuilder);
	}

	partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
