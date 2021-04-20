﻿// <auto-generated />
using System;
using ErabliereApi.Depot.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ErabliereApi.Depot.Sql.Migrations
{
    [DbContext(typeof(ErabliereDbContext))]
    [Migration("20210419215451_DonneeCapteurDateCreation")]
    partial class DonneeCapteurDateCreation
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.5")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("ErabliereApi.Donnees.Alerte", b =>
                {
                    b.Property<int?>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("EnvoyerA")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int?>("IdErabliere")
                        .HasColumnType("int");

                    b.Property<string>("NiveauBassinThresholdHight")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("NiveauBassinThresholdLow")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("TemperatureThresholdHight")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("TemperatureThresholdLow")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("VacciumThresholdHight")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("VacciumThresholdLow")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.ToTable("Alertes");
                });

            modelBuilder.Entity("ErabliereApi.Donnees.Baril", b =>
                {
                    b.Property<int?>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTimeOffset?>("DF")
                        .HasColumnType("datetimeoffset");

                    b.Property<int?>("IdErabliere")
                        .HasColumnType("int");

                    b.Property<string>("Q")
                        .HasMaxLength(15)
                        .HasColumnType("nvarchar(15)");

                    b.Property<string>("QE")
                        .HasMaxLength(15)
                        .HasColumnType("nvarchar(15)");

                    b.HasKey("Id");

                    b.ToTable("Barils");
                });

            modelBuilder.Entity("ErabliereApi.Donnees.Capteur", b =>
                {
                    b.Property<int?>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTimeOffset?>("DC")
                        .HasColumnType("datetimeoffset");

                    b.Property<int?>("ErabliereId")
                        .HasColumnType("int");

                    b.Property<int?>("IdErabliere")
                        .HasColumnType("int");

                    b.Property<string>("Nom")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.HasIndex("ErabliereId");

                    b.ToTable("Capteurs");
                });

            modelBuilder.Entity("ErabliereApi.Donnees.Dompeux", b =>
                {
                    b.Property<int?>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTimeOffset?>("DD")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset?>("DF")
                        .HasColumnType("datetimeoffset");

                    b.Property<int?>("IdErabliere")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("T")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("Id");

                    b.ToTable("Dompeux");
                });

            modelBuilder.Entity("ErabliereApi.Donnees.Donnee", b =>
                {
                    b.Property<int?>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTimeOffset?>("D")
                        .HasColumnType("datetimeoffset");

                    b.Property<int?>("IdErabliere")
                        .HasColumnType("int");

                    b.Property<int?>("Iddp")
                        .HasColumnType("int");

                    b.Property<short?>("NB")
                        .HasColumnType("smallint");

                    b.Property<int>("Nboc")
                        .HasColumnType("int");

                    b.Property<int?>("PI")
                        .HasColumnType("int");

                    b.Property<short?>("T")
                        .HasColumnType("smallint");

                    b.Property<short?>("V")
                        .HasColumnType("smallint");

                    b.HasKey("Id");

                    b.ToTable("Donnees");
                });

            modelBuilder.Entity("ErabliereApi.Donnees.DonneeCapteur", b =>
                {
                    b.Property<Guid?>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("CapteurId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("D")
                        .HasColumnType("datetimeoffset");

                    b.Property<int?>("IdCapteur")
                        .HasColumnType("int");

                    b.Property<short>("Valeur")
                        .HasColumnType("smallint");

                    b.HasKey("Id");

                    b.HasIndex("CapteurId");

                    b.ToTable("DonneesCapteur");
                });

            modelBuilder.Entity("ErabliereApi.Donnees.Erabliere", b =>
                {
                    b.Property<int?>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<bool?>("AfficherSectionBaril")
                        .HasColumnType("bit");

                    b.Property<int?>("IndiceOrdre")
                        .HasColumnType("int");

                    b.Property<string>("IpRule")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Nom")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.ToTable("Erabliere");
                });

            modelBuilder.Entity("ErabliereApi.Donnees.Capteur", b =>
                {
                    b.HasOne("ErabliereApi.Donnees.Erabliere", "Erabliere")
                        .WithMany("Capteurs")
                        .HasForeignKey("ErabliereId");

                    b.Navigation("Erabliere");
                });

            modelBuilder.Entity("ErabliereApi.Donnees.DonneeCapteur", b =>
                {
                    b.HasOne("ErabliereApi.Donnees.Capteur", "Capteur")
                        .WithMany("DonneesCapteur")
                        .HasForeignKey("CapteurId");

                    b.Navigation("Capteur");
                });

            modelBuilder.Entity("ErabliereApi.Donnees.Capteur", b =>
                {
                    b.Navigation("DonneesCapteur");
                });

            modelBuilder.Entity("ErabliereApi.Donnees.Erabliere", b =>
                {
                    b.Navigation("Capteurs");
                });
#pragma warning restore 612, 618
        }
    }
}
