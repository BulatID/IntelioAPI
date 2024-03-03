using Microsoft.EntityFrameworkCore;
using System.IO;

public class NewsDbContext : DbContext
{
    public DbSet<News>? News { get; set; }
    public DbSet<RssSource>? RssSources { get; set; }
    public DbSet<TGUser>? TGuser { get; set; }
    public DbSet<Parameters>? Parameters { get; set; }
    public DbSet<StopWords>? StopWords { get; set; }
    public DbSet<Rates>? Rates { get; set; }
    public DbSet<Favorites>? Favorites { get; set; }
    public DbSet<ApiKeys>? ApiKeys { get; set; }
    public DbSet<PayList>? PayList { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        string databasePath = "data.db";
        options.UseSqlite($"Data Source={databasePath}");
    }

    public void EnsureDbCreated()
    {
        Database.EnsureCreated();
    }
}
