using Microsoft.EntityFrameworkCore;
using Mikibot.Database.Model;
using MySqlConnector;

namespace Mikibot.Database;

public class MikibotDatabaseContext : DbContext
{
    public MikibotDatabaseContext(DbContextOptions<MikibotDatabaseContext> options) : base(options) {}
    
    public MikibotDatabaseContext(MySqlConfiguration mySqlConfiguration)
    {
        MySqlConfiguration = mySqlConfiguration;
    }

    public MySqlConfiguration MySqlConfiguration { get; }

    public DbSet<LiveStatus> LiveStatuses { get; set; }
    public DbSet<FollowerStatistic> FollowerStatistic { get; set; }
    public DbSet<StatisticReportLog> StatisticReportLogs { get; set; }
    public DbSet<LiveDanmaku> LiveDanmakus { get; set; }
    public DbSet<LiveUserInteractiveLog> LiveUserInteractiveLogs { get; set; }
    public DbSet<LiveBuyGuardLog> LiveBuyGuardLogs { get; set; }
    public DbSet<LiveGuardEnterLog> LiveGuardEnterLogs { get; set; }
    public DbSet<LiveGift> LiveGifts { get; set; }
    public DbSet<LiveGiftCombo> LiveGiftCombos { get; set; }
    public DbSet<LiveSuperChat> LiveSuperChats { get; set; }
    public DbSet<LiveStreamRecord> LiveStreamRecords { get; set; }
    public DbSet<VoxList> VoxList { get; set; }
    
    public DbSet<SubscriptionFansTrends> SubscriptionFansTrends { get; set; }
    public DbSet<SubscriptionLiveStart> SubscriptionLiveStarts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var cs = new MySqlConnectionStringBuilder()
        {
            Database = MySqlConfiguration.Database,
            Server = MySqlConfiguration.Host,
            Port = MySqlConfiguration.Port,
            Password = MySqlConfiguration.Password,
            UserID = MySqlConfiguration.User,
        }.ToString();
        optionsBuilder.UseMySql(cs, ServerVersion.AutoDetect(cs));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<LiveStatus>(model =>
        {
            model.HasKey(model => model.Id);
            model.HasIndex(model => model.Bid);
            model.Property(model => model.Id).ValueGeneratedOnAdd();
            model.Property(model => model.Bid).IsRequired();
            model.Property(model => model.Status).IsRequired();
            model.Property(model => model.CreatedAt).ValueGeneratedOnAdd();
            model.Property(model => model.UpdatedAt).ValueGeneratedOnUpdate();
            model.Property(model => model.Notified).HasDefaultValue(false);
        });

        modelBuilder.Entity<FollowerStatistic>(model =>
        {
            model.HasKey(model => model.Id);
            model.HasIndex(model => new { model.Bid, model.CreatedAt });
            model.Property(model => model.Bid).IsRequired();
            model.Property(model => model.FollowerCount).IsRequired();
            model.Property(model => model.CreatedAt).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<LiveDanmaku>(model =>
        {
            model.HasKey(m => m.Id);
            model.HasIndex(m => new { m.Bid, m.UserId });
            model.HasIndex(m => new { m.Bid, m.FansTagUserId });
            model.HasIndex(m => new { m.Bid, m.SentAt });
            model.Property(m => m.CreatedAt).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<LiveUserInteractiveLog>(model =>
        {
            model.HasKey(m => m.Id);
            model.HasIndex(m => new { m.Bid, m.UserId });
            model.HasIndex(m => new { m.Bid, m.FansTagUserId });
            model.HasIndex(m => new { m.Bid, m.InteractedAt });
            model.Property(m => m.CreatedAt).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<LiveBuyGuardLog>(model =>
        {
            model.HasKey(m => m.Id);
            model.HasIndex(m => new { m.Bid, m.Uid });
            model.HasIndex(m => new { m.Bid, m.BoughtAt });
            model.Property(m => m.CreatedAt).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<LiveGuardEnterLog>(model =>
        {
            model.HasKey(m => m.Id);
            model.HasIndex(m => new { m.Bid, m.UserId });
            model.HasIndex(m => new { m.Bid, m.EnteredAt });
            model.Property(m => m.CreatedAt).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<LiveGift>(model =>
        {
            model.HasKey(m => m.Id);
            model.HasIndex(m => new { m.Bid, m.Uid });
            model.HasIndex(m => new { m.Bid, m.SentAt });
            model.HasIndex(m => new { m.Bid, m.ComboId });
            model.Property(m => m.CreatedAt).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<LiveGiftCombo>(model =>
        {
            model.HasKey(m => m.Id);
            model.HasIndex(m => new { m.Bid, m.Uid });
            model.HasIndex(m => new { m.Bid, m.CreatedAt });
            model.HasIndex(m => new { m.Bid, m.ComboId });
            model.Property(m => m.CreatedAt).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<LiveSuperChat>(model =>
        {
            model.HasKey(m => m.Id);
            model.HasIndex(m => new { m.Bid, m.Uid });
            model.Property(m => m.CreatedAt).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<LiveStreamRecord>(model =>
        {
            model.HasKey(m => m.Id);
            model.HasIndex(m => new { m.Bid, m.CreatedAt, m.RecordStoppedAt });
        });

        modelBuilder.Entity<VoxList>(model =>
        {
            model.ToTable("暗杀名单");
            model.HasKey(m => m.Id);
        });
    }
}