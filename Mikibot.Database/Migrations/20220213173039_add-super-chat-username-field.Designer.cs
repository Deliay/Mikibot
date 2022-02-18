﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Mikibot.Database;

#nullable disable

namespace Mikibot.Migrations
{
    [DbContext(typeof(MikibotDatabaseContext))]
    [Migration("20220213173039_add-super-chat-username-field")]
    partial class addsuperchatusernamefield
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Mikibot.Database.Model.FollowerStatistic", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Bid")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

                    b.Property<int>("FollowerCount")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Bid", "CreatedAt");

                    b.ToTable("FollowerStatistic");
                });

            modelBuilder.Entity("Mikibot.Database.Model.LiveBuyGuardLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("Bid")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("BoughtAt")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

                    b.Property<string>("GiftName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("GuardLevel")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("Price")
                        .HasColumnType("int");

                    b.Property<int>("Uid")
                        .HasColumnType("int");

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
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("Bid")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

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

                    b.Property<int>("UserId")
                        .HasColumnType("int");

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
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("Bid")
                        .HasColumnType("int");

                    b.Property<string>("CoinType")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("ComboId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

                    b.Property<int>("DiscountPrice")
                        .HasColumnType("int");

                    b.Property<string>("GiftName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTimeOffset>("SentAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("Uid")
                        .HasColumnType("int");

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
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("Bid")
                        .HasColumnType("int");

                    b.Property<string>("ComboId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<int>("ComboNum")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

                    b.Property<string>("GiftName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("TotalCoin")
                        .HasColumnType("int");

                    b.Property<int>("Uid")
                        .HasColumnType("int");

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

                    b.Property<int>("Bid")
                        .HasColumnType("int");

                    b.Property<string>("CopyWriting")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

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
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Bid")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Cover")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

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

            modelBuilder.Entity("Mikibot.Database.Model.LiveSuperChat", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("Bid")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

                    b.Property<int>("MedalGuardLevel")
                        .HasColumnType("int");

                    b.Property<int>("MedalLevel")
                        .HasColumnType("int");

                    b.Property<string>("MedalName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("MedalUserId")
                        .HasColumnType("int");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("Price")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("SentAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("Uid")
                        .HasColumnType("int");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("Bid", "Uid");

                    b.ToTable("LiveSuperChats");
                });

            modelBuilder.Entity("Mikibot.Database.Model.LiveUserInteractiveLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("Bid")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

                    b.Property<int>("FansTagUserId")
                        .HasColumnType("int");

                    b.Property<int>("GuardLevel")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("InteractedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("MedalLevel")
                        .HasColumnType("int");

                    b.Property<string>("MedalName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

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
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

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
#pragma warning restore 612, 618
        }
    }
}
