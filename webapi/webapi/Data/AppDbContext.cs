using Microsoft.EntityFrameworkCore;
using webapi.Models;

namespace webapi.Data;

public partial class AppDbContext : DbContext
{
	public const string DATETIME_STRING_FORMAT = "yyyy-MM-dd HH:mm:ss";

	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	{
		Database.EnsureCreated();
	}

	public virtual DbSet<CheckersHistory> CheckersHistories { get; set; }

	public virtual DbSet<CheckersUser> CheckersUsers { get; set; }

	public virtual DbSet<User> Users { get; set; }

	public virtual DbSet<UserRefreshToken> UserRefreshTokens { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<CheckersHistory>(entity =>
		{
			entity.ToTable("Checkers_History");

			entity.Property(e => e.Id).HasColumnName("ID");
			entity.Property(e => e.LooserId).HasColumnName("LooserID");
			entity.Property(e => e.WinnerId).HasColumnName("WinnerID");

			entity.HasOne(d => d.Looser).WithMany(p => p.CheckersHistoryLoosers).HasForeignKey(d => d.LooserId);

			entity.HasOne(d => d.Winner).WithMany(p => p.CheckersHistoryWinners).HasForeignKey(d => d.WinnerId);
		});

		modelBuilder.Entity<CheckersUser>(entity =>
		{
			entity.HasKey(e => e.UserId);

			entity.ToTable("Checkers_User");

			entity.Property(e => e.UserId)
				.ValueGeneratedNever()
				.HasColumnName("UserID");

			entity.HasOne(d => d.User).WithOne(p => p.CheckersUser).HasForeignKey<CheckersUser>(d => d.UserId);
		});

		modelBuilder.Entity<User>(entity =>
		{
			entity.HasIndex(e => e.Id, "IX_Users_ID").IsUnique();

			entity.HasIndex(e => e.Name, "IX_Users_Name").IsUnique();

			entity.Property(e => e.Id).HasColumnName("ID");
		});

		modelBuilder.Entity<UserRefreshToken>(entity =>
		{
			entity.HasKey(e => new { e.UserId, e.DeviceId });

			entity.ToTable("User_RefreshToken");

			entity.Property(e => e.UserId).HasColumnName("UserID");
			entity.Property(e => e.DeviceId).HasColumnName("DeviceID");

			entity.HasOne(d => d.User).WithMany(p => p.UserRefreshTokens).HasForeignKey(d => d.UserId);
		});

		OnModelCreatingPartial(modelBuilder);
	}

	partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
