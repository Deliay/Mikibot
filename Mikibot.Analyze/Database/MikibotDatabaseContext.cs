using Microsoft.EntityFrameworkCore;
using Mikibot.Analyze.Database.Model;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Database
{
    public class MikibotDatabaseContext : DbContext
    {
        public MikibotDatabaseContext(MySqlConfiguration mySqlConfiguration)
        {
            MySqlConfiguration = mySqlConfiguration;
        }

        public MySqlConfiguration MySqlConfiguration { get; }

        public DbSet<LiveStatus> LiveStatuses { get; set; }
        public DbSet<FollowerStatistic> FollowerStatistic { get; set; }
        public DbSet<StatisticReportLog> StatisticReportLogs { get; set; }

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
        }
    }
}
