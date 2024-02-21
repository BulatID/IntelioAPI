using Microsoft.EntityFrameworkCore;
using System.IO;

public class NewsDbContext : DbContext
{
    public DbSet<News> News { get; set; }
    public DbSet<RssSource> RssSources { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        string databasePath = "news.db";
        options.UseSqlite($"Data Source={databasePath}");
    }

    public void EnsureDbCreated()
    {
        Database.EnsureCreated(); // Убеждаемся, что база данных создана
    }
}
