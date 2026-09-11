using Microsoft.EntityFrameworkCore;

namespace DiceMiniGames.Data;

public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<PlayerScore> PlayerScores => Set<PlayerScore>();
    public DbSet<GameResult> GameResults => Set<GameResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerScore>(e =>
        {
            e.HasKey(p => new { p.UserId, p.GameType });
            e.HasIndex(p => new { p.GameType, p.TotalScore });
        });

        modelBuilder.Entity<GameResult>(e =>
        {
            e.HasKey(g => g.Id);
            e.HasIndex(g => g.UserId);
        });
    }
}

public class PlayerScore
{
    public long UserId { get; set; }
    public string UserName { get; set; } = "";
    public string GameType { get; set; } = "";
    public int TotalScore { get; set; }
    public int GamesPlayed { get; set; }
    public int BestResult { get; set; }
    public DateTime LastPlayed { get; set; }
}

public class GameResult
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public string GameType { get; set; } = "";
    public int DiceValue { get; set; }
    public int PointsEarned { get; set; }
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
}
