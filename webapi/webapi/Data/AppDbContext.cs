using Microsoft.EntityFrameworkCore;
using webapi.Models;

namespace webapi.Data;

public partial class AppDbContext : DbContext
{
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
			entity.Property(e => e.UserId).HasColumnName("UserID");

			entity.HasOne(d => d.User).WithMany(p => p.CheckersHistories)
				.HasForeignKey(d => d.UserId)
				.OnDelete(DeleteBehavior.ClientSetNull);
		});

		modelBuilder.Entity<CheckersUser>(entity =>
		{
			entity.HasKey(e => e.UserId);

			entity.ToTable("Checkers_User");

			entity.Property(e => e.UserId)
				.ValueGeneratedNever()
				.HasColumnName("UserID");

			entity.HasOne(d => d.User).WithOne(p => p.CheckersUser)
				.HasForeignKey<CheckersUser>(d => d.UserId)
				.OnDelete(DeleteBehavior.ClientSetNull);
		});

		modelBuilder.Entity<User>(entity =>
		{
			entity.HasIndex(e => e.Id, "IX_Users_ID").IsUnique();

			entity.HasIndex(e => e.Name, "IX_Users_Name").IsUnique();

			entity.Property(e => e.Id).HasColumnName("ID");
		});

		modelBuilder.Entity<UserRefreshToken>(entity =>
		{
			entity.HasKey(e => new { e.UserId, e.RefreshToken });

			entity.ToTable("User_RefreshToken");

			entity.Property(e => e.UserId).HasColumnName("UserID");

			entity.HasOne(d => d.User).WithMany(p => p.UserRefreshTokens)
				.HasForeignKey(d => d.UserId)
				.OnDelete(DeleteBehavior.ClientSetNull);
		});

		OnModelCreatingPartial(modelBuilder);
	}

	partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
