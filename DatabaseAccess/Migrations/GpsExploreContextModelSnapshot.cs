﻿// <auto-generated />
using System;
using DatabaseAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

namespace DatabaseAccess.Migrations
{
    [DbContext(typeof(GpsExploreContext))]
    partial class GpsExploreContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseIdentityColumns(1, 1)
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.0-rc.1.20451.13");

            modelBuilder.Entity("DatabaseAccess.DbTables+AreaType", b =>
                {
                    b.Property<int>("AreaTypeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn(1, 1);

                    b.Property<string>("AreaName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OsmTags")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("AreaTypeId");

                    b.ToTable("AreaTypes");
                });

            modelBuilder.Entity("DatabaseAccess.DbTables+MapData", b =>
                {
                    b.Property<long>("MapDataId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .UseIdentityColumn(1, 1);

                    b.Property<long?>("NodeId")
                        .HasColumnType("bigint");

                    b.Property<long?>("RelationId")
                        .HasColumnType("bigint");

                    b.Property<long?>("WayId")
                        .HasColumnType("bigint");

                    b.Property<string>("name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Geometry>("place")
                        .HasColumnType("geography");

                    b.Property<string>("type")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("MapDataId");

                    b.HasIndex("WayId");

                    b.ToTable("MapData");
                });

            modelBuilder.Entity("DatabaseAccess.DbTables+MinimumNode", b =>
                {
                    b.Property<long?>("MinimumNodeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .UseIdentityColumn(1, 1);

                    b.Property<double?>("Lat")
                        .HasColumnType("float");

                    b.Property<double?>("Lon")
                        .HasColumnType("float");

                    b.Property<long>("NodeId")
                        .HasColumnType("bigint");

                    b.HasKey("MinimumNodeId");

                    b.ToTable("MinimumNodes");
                });

            modelBuilder.Entity("DatabaseAccess.DbTables+MinimumRelation", b =>
                {
                    b.Property<long>("MinimumRelationId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .UseIdentityColumn(1, 1);

                    b.Property<long>("RelationId")
                        .HasColumnType("bigint");

                    b.HasKey("MinimumRelationId");

                    b.ToTable("minimumRelations");
                });

            modelBuilder.Entity("DatabaseAccess.DbTables+MinimumWay", b =>
                {
                    b.Property<long?>("MinimumWayId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .UseIdentityColumn(1, 1);

                    b.Property<long>("WayId")
                        .HasColumnType("bigint");

                    b.HasKey("MinimumWayId");

                    b.ToTable("MinimumWays");
                });

            modelBuilder.Entity("DatabaseAccess.DbTables+PerformanceInfo", b =>
                {
                    b.Property<int>("PerformanceInfoID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn(1, 1);

                    b.Property<DateTime>("calledAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("functionName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("notes")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("runTime")
                        .HasColumnType("bigint");

                    b.HasKey("PerformanceInfoID");

                    b.ToTable("PerformanceInfo");
                });

            modelBuilder.Entity("DatabaseAccess.DbTables+PlayerData", b =>
                {
                    b.Property<int>("PlayerDataID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn(1, 1);

                    b.Property<int>("DateLastTrophyBought")
                        .HasColumnType("int");

                    b.Property<int>("altitudeSpread")
                        .HasColumnType("int");

                    b.Property<int>("cellVisits")
                        .HasColumnType("int");

                    b.Property<string>("deviceID")
                        .HasColumnType("nvarchar(450)");

                    b.Property<double>("distance")
                        .HasColumnType("float");

                    b.Property<DateTime>("lastSyncTime")
                        .HasColumnType("datetime2");

                    b.Property<double>("maxSpeed")
                        .HasColumnType("float");

                    b.Property<int>("score")
                        .HasColumnType("int");

                    b.Property<int>("t10Cells")
                        .HasColumnType("int");

                    b.Property<int>("t8Cells")
                        .HasColumnType("int");

                    b.Property<int>("timePlayed")
                        .HasColumnType("int");

                    b.Property<double>("totalSpeed")
                        .HasColumnType("float");

                    b.HasKey("PlayerDataID");

                    b.HasIndex("deviceID");

                    b.ToTable("PlayerData");
                });

            modelBuilder.Entity("DatabaseAccess.DbTables+PremadeResults", b =>
                {
                    b.Property<long>("PremadeResultsId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .UseIdentityColumn(1, 1);

                    b.Property<string>("Data")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PlusCode6")
                        .HasMaxLength(6)
                        .HasColumnType("nvarchar(6)");

                    b.HasKey("PremadeResultsId");

                    b.HasIndex("PlusCode6");

                    b.ToTable("PremadeResults");
                });

            modelBuilder.Entity("MinimumNodeMinimumWay", b =>
                {
                    b.Property<long>("NodesMinimumNodeId")
                        .HasColumnType("bigint");

                    b.Property<long>("WaysMinimumWayId")
                        .HasColumnType("bigint");

                    b.HasKey("NodesMinimumNodeId", "WaysMinimumWayId");

                    b.HasIndex("WaysMinimumWayId");

                    b.ToTable("MinimumNodeMinimumWay");
                });

            modelBuilder.Entity("MinimumNodeMinimumWay", b =>
                {
                    b.HasOne("DatabaseAccess.DbTables+MinimumNode", null)
                        .WithMany()
                        .HasForeignKey("NodesMinimumNodeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DatabaseAccess.DbTables+MinimumWay", null)
                        .WithMany()
                        .HasForeignKey("WaysMinimumWayId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
