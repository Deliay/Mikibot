﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Mikibot.Database;

#nullable disable

namespace Mikibot.Migrations
{
    [DbContext(typeof(MikibotDatabaseContext))]
    partial class MikibotDatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("Mikibot.Database.Model.FollowerStatistic", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Bid")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<DateTimeOffset>("CreatedAt"));

                    b.Property<int>("FollowerCount")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Bid", "CreatedAt");

                    b.ToTable("FollowerStatistic");
                });

            modelBuilder.Entity("Mikibot.Database.Model.LiveBuyGuardLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Bid")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<DateTimeOffset>("BoughtAt")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<DateTimeOffset>("CreatedAt"));

                    b.Property<string>("GiftName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("GuardLevel")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("Price")
                        .HasColumnType("int");

                    b.Property<string>("Uid")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("Bid", "BoughtAt");

                    b.HasIndex("Bid", "Uid");

                    b.ToTable("LiveBuyGuardLogs");
                });

            modelBuilder.Entity("Mikibot.Database.Model.LiveDanmaku", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Bid")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<DateTimeOffset>("CreatedAt"));

                    b.Property<int>("FansLevel")
                        .HasColumnType("int");

                    b.Property<string>("FansTag")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("FansTagUserId")
                        .HasColumnType("int");

                    b.Property<string>("FansTagUserName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Msg")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTimeOffset>("SentAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("Bid", "FansTagUserId");

                    b.HasIndex("Bid", "SentAt");

                    b.HasIndex("Bid", "UserId");

                    b.ToTable("LiveDanmakus");
                });

            modelBuilder.Entity("Mikibot.Database.Model.LiveGift", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Bid")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("CoinType")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("ComboId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<DateTimeOffset>("CreatedAt"));

                    b.Property<int>("DiscountPrice")
                        .HasColumnType("int");

                    b.Property<string>("GiftName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTimeOffset>("SentAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Uid")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("Bid", "ComboId");

                    b.HasIndex("Bid", "SentAt");

                    b.HasIndex("Bid", "Uid");

                    b.ToTable("LiveGifts");
                });

            modelBuilder.Entity("Mikibot.Database.Model.LiveGiftCombo", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Bid")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("ComboId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<int>("ComboNum")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<DateTimeOffset>("CreatedAt"));

                    b.Property<string>("GiftName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("TotalCoin")
                        .HasColumnType("int");

                    b.Property<string>("Uid")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("Bid", "ComboId");

                    b.HasIndex("Bid", "CreatedAt");

                    b.HasIndex("Bid", "Uid");

                    b.ToTable("LiveGiftCombos");
                });

            modelBuilder.Entity("Mikibot.Database.Model.LiveGuardEnterLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("Bid")
                        .HasColumnType("int");

                    b.Property<string>("CopyWriting")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<DateTimeOffset>("CreatedAt"));

                    b.Property<DateTimeOffset>("EnteredAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("GuardLevel")
                        .HasColumnType("int");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Bid", "EnteredAt");

                    b.HasIndex("Bid", "UserId");

                    b.ToTable("LiveGuardEnterLogs");
                });

            modelBuilder.Entity("Mikibot.Database.Model.LiveStatus", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Bid")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Cover")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<DateTimeOffset>("CreatedAt"));

                    b.Property<int>("FollowerCount")
                        .HasColumnType("int");

                    b.Property<bool>("Notified")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("tinyint(1)")
                        .HasDefaultValue(false);

                    b.Property<DateTimeOffset>("NotifiedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("StatusChangedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .ValueGeneratedOnUpdate()
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("Bid");

                    b.ToTable("LiveStatuses");
                });

            modelBuilder.Entity("Mikibot.Database.Model.LiveStreamRecord", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Bid")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("Duration")
                        .HasColumnType("int");

                    b.Property<string>("LocalFileName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTimeOffset>("RecordStoppedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("Reserve")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("Id");

                    b.HasIndex("Bid", "CreatedAt", "RecordStoppedAt");

                    b.ToTable("LiveStreamRecords");
                });

            modelBuilder.Entity("Mikibot.Database.Model.LiveSuperChat", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Bid")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<DateTimeOffset>("CreatedAt"));

                    b.Property<int>("MedalGuardLevel")
                        .HasColumnType("int");

                    b.Property<int>("MedalLevel")
                        .HasColumnType("int");

                    b.Property<string>("MedalName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("MedalUserId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("Price")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("SentAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Uid")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("Bid", "Uid");

                    b.ToTable("LiveSuperChats");
                });

            modelBuilder.Entity("Mikibot.Database.Model.LiveUserInteractiveLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Bid")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<DateTimeOffset>("CreatedAt"));

                    b.Property<string>("FansTagUserId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<int>("GuardLevel")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("InteractedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("MedalLevel")
                        .HasColumnType("int");

                    b.Property<string>("MedalName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("Bid", "FansTagUserId");

                    b.HasIndex("Bid", "InteractedAt");

                    b.HasIndex("Bid", "UserId");

                    b.ToTable("LiveUserInteractiveLogs");
                });

            modelBuilder.Entity("Mikibot.Database.Model.StatisticReportLog", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Bid")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("ReportIdentity")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTimeOffset>("ReportedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("Bid");

                    b.ToTable("StatisticReportLogs");
                });

            modelBuilder.Entity("Mikibot.Database.Model.SubscriptionFansTrends", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("GroupId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("TargetFansCount")
                        .HasColumnType("int");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("SubscriptionFansTrends");
                });

            modelBuilder.Entity("Mikibot.Database.Model.SubscriptionLiveStart", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<long>("Id"));

                    b.Property<bool>("EnabledFansTrendingStatistics")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("GroupId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("RoomId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("SubscriptionLiveStarts");
                });

            modelBuilder.Entity("Mikibot.Database.Model.VoxList", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Bid")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("暗杀名单", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
