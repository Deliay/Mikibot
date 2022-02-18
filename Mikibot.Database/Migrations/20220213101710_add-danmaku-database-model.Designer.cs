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
    [Migration("20220213101710_add-danmaku-database-model")]
    partial class adddanmakudatabasemodel
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

            modelBuilder.Entity("Mikibot.Database.Model.LiveDanmaku", b =>
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

                    b.Property<DateTimeOffset>("SentAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("Bid", "FansTagUserId");

                    b.HasIndex("Bid", "UserId");

                    b.ToTable("LiveDanmakus");
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
