﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SCMM.Web.Server.Data;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    [DbContext(typeof(SteamDbContext))]
    partial class SteamDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("SCMM.Web.Server.Domain.Models.Steam.SteamApp", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("IconLargeUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("IconUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SteamId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("SteamApps");
                });

            modelBuilder.Entity("SCMM.Web.Server.Domain.Models.Steam.SteamAssetDescription", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("BackgroundColour")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ForegroundColour")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("IconLargeUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("IconUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset?>("LastCheckedOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SteamId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid?>("WorkshopFileId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("WorkshopFileId");

                    b.ToTable("SteamAssetDescriptions");
                });

            modelBuilder.Entity("SCMM.Web.Server.Domain.Models.Steam.SteamAssetWorkshopFile", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("CreatedOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<Guid?>("CreatorId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Favourited")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("LastCheckedOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("SteamId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Subscriptions")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("UpdatedOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("Views")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("CreatorId");

                    b.ToTable("SteamAssetWorkshopFiles");
                });

            modelBuilder.Entity("SCMM.Web.Server.Domain.Models.Steam.SteamCurrency", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PrefixText")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SteamId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SuffixText")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("SteamCurrencies");
                });

            modelBuilder.Entity("SCMM.Web.Server.Domain.Models.Steam.SteamInventoryItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AppId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("BuyPrice")
                        .HasColumnType("int");

                    b.Property<Guid?>("CurrencyId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("DescriptionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<string>("SteamId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid?>("SteamProfileId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("AppId");

                    b.HasIndex("CurrencyId");

                    b.HasIndex("DescriptionId");

                    b.HasIndex("SteamProfileId");

                    b.ToTable("SteamInventoryItems");
                });

            modelBuilder.Entity("SCMM.Web.Server.Domain.Models.Steam.SteamLanguage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SteamId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("SteamLanguages");
                });

            modelBuilder.Entity("SCMM.Web.Server.Domain.Models.Steam.SteamMarketItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AppId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("BuyNowPrice")
                        .HasColumnType("int");

                    b.Property<int>("BuyNowPriceDelta")
                        .HasColumnType("int");

                    b.Property<Guid?>("CurrencyId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Demand")
                        .HasColumnType("int");

                    b.Property<Guid>("DescriptionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset?>("LastCheckedOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("ResellPrice")
                        .HasColumnType("int");

                    b.Property<int>("ResellProfit")
                        .HasColumnType("int");

                    b.Property<int>("ResellTax")
                        .HasColumnType("int");

                    b.Property<string>("SteamId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Supply")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AppId");

                    b.HasIndex("CurrencyId");

                    b.HasIndex("DescriptionId");

                    b.ToTable("SteamMarketItems");
                });

            modelBuilder.Entity("SCMM.Web.Server.Domain.Models.Steam.SteamProfile", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AvatarLargeUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("AvatarUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Country")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProfileId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SteamId")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("SteamProfiles");
                });

            modelBuilder.Entity("SCMM.Web.Server.Domain.Models.Steam.SteamStoreItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("AppId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("CurrencyId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("DescriptionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset?>("FirstReleasedOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("SteamId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("StorePrice")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AppId");

                    b.HasIndex("CurrencyId");

                    b.HasIndex("DescriptionId");

                    b.ToTable("SteamStoreItems");
                });

            modelBuilder.Entity("SCMM.Web.Server.Domain.Models.Steam.SteamAssetDescription", b =>
                {
                    b.HasOne("SCMM.Web.Server.Domain.Models.Steam.SteamAssetWorkshopFile", "WorkshopFile")
                        .WithMany()
                        .HasForeignKey("WorkshopFileId");

                    b.OwnsOne("SCMM.Web.Server.Data.Types.PersistableStringCollection", "Tags", b1 =>
                        {
                            b1.Property<Guid>("SteamAssetDescriptionId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Serialised")
                                .HasColumnType("nvarchar(max)");

                            b1.HasKey("SteamAssetDescriptionId");

                            b1.ToTable("SteamAssetDescriptions");

                            b1.WithOwner()
                                .HasForeignKey("SteamAssetDescriptionId");
                        });
                });

            modelBuilder.Entity("SCMM.Web.Server.Domain.Models.Steam.SteamAssetWorkshopFile", b =>
                {
                    b.HasOne("SCMM.Web.Server.Domain.Models.Steam.SteamProfile", "Creator")
                        .WithMany("WorkshopFiles")
                        .HasForeignKey("CreatorId");
                });

            modelBuilder.Entity("SCMM.Web.Server.Domain.Models.Steam.SteamInventoryItem", b =>
                {
                    b.HasOne("SCMM.Web.Server.Domain.Models.Steam.SteamApp", "App")
                        .WithMany()
                        .HasForeignKey("AppId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SCMM.Web.Server.Domain.Models.Steam.SteamCurrency", "Currency")
                        .WithMany()
                        .HasForeignKey("CurrencyId");

                    b.HasOne("SCMM.Web.Server.Domain.Models.Steam.SteamAssetDescription", "Description")
                        .WithMany()
                        .HasForeignKey("DescriptionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SCMM.Web.Server.Domain.Models.Steam.SteamProfile", null)
                        .WithMany("InventoryItems")
                        .HasForeignKey("SteamProfileId");
                });

            modelBuilder.Entity("SCMM.Web.Server.Domain.Models.Steam.SteamMarketItem", b =>
                {
                    b.HasOne("SCMM.Web.Server.Domain.Models.Steam.SteamApp", "App")
                        .WithMany("MarketItems")
                        .HasForeignKey("AppId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SCMM.Web.Server.Domain.Models.Steam.SteamCurrency", "Currency")
                        .WithMany()
                        .HasForeignKey("CurrencyId");

                    b.HasOne("SCMM.Web.Server.Domain.Models.Steam.SteamAssetDescription", "Description")
                        .WithMany()
                        .HasForeignKey("DescriptionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsMany("SCMM.Web.Server.Domain.Models.Steam.SteamMarketItemOrder", "BuyOrders", b1 =>
                        {
                            b1.Property<Guid>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("uniqueidentifier");

                            b1.Property<int>("Price")
                                .HasColumnType("int");

                            b1.Property<int>("Quantity")
                                .HasColumnType("int");

                            b1.Property<Guid>("SteamMarketItemId")
                                .HasColumnType("uniqueidentifier");

                            b1.HasKey("Id");

                            b1.HasIndex("SteamMarketItemId");

                            b1.ToTable("SteamMarketItems_BuyOrders");

                            b1.WithOwner()
                                .HasForeignKey("SteamMarketItemId");
                        });

                    b.OwnsMany("SCMM.Web.Server.Domain.Models.Steam.SteamMarketItemOrder", "SellOrders", b1 =>
                        {
                            b1.Property<Guid>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("uniqueidentifier");

                            b1.Property<int>("Price")
                                .HasColumnType("int");

                            b1.Property<int>("Quantity")
                                .HasColumnType("int");

                            b1.Property<Guid>("SteamMarketItemId")
                                .HasColumnType("uniqueidentifier");

                            b1.HasKey("Id");

                            b1.HasIndex("SteamMarketItemId");

                            b1.ToTable("SteamMarketItems_SellOrders");

                            b1.WithOwner()
                                .HasForeignKey("SteamMarketItemId");
                        });
                });

            modelBuilder.Entity("SCMM.Web.Server.Domain.Models.Steam.SteamStoreItem", b =>
                {
                    b.HasOne("SCMM.Web.Server.Domain.Models.Steam.SteamApp", "App")
                        .WithMany("StoreItems")
                        .HasForeignKey("AppId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SCMM.Web.Server.Domain.Models.Steam.SteamCurrency", "Currency")
                        .WithMany()
                        .HasForeignKey("CurrencyId");

                    b.HasOne("SCMM.Web.Server.Domain.Models.Steam.SteamAssetDescription", "Description")
                        .WithMany()
                        .HasForeignKey("DescriptionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
