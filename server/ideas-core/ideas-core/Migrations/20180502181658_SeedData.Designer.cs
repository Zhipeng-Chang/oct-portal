﻿// <auto-generated />
using CoE.Ideas.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;

namespace CoE.Ideas.Core.Migrations
{
    [DbContext(typeof(InitiativeContext))]
    [Migration("20180502181658_SeedData")]
    partial class SeedData
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.2-rtm-10011")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("CoE.Ideas.Core.Data.Initiative", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("ApexId");

                    b.Property<int?>("AssigneeId");

                    b.Property<string>("BusinessCaseUrl")
                        .HasMaxLength(2048);

                    b.Property<DateTimeOffset>("CreatedDate");

                    b.Property<string>("Description")
                        .IsRequired();

                    b.Property<string>("InvestmentRequestFormUrl")
                        .HasMaxLength(2048);

                    b.Property<int>("Status");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<Guid>("Uid");

                    b.Property<string>("WorkOrderId")
                        .HasMaxLength(128);

                    b.HasKey("Id");

                    b.HasIndex("WorkOrderId")
                        .IsUnique()
                        .HasFilter("[WorkOrderId] IS NOT NULL");

                    b.ToTable("Initiatives");
                });

            modelBuilder.Entity("CoE.Ideas.Core.Data.InitiativeStatusHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("ExpectedExitDateUtc");

                    b.Property<int?>("InitiativeId");

                    b.Property<int?>("PersonId");

                    b.Property<int>("Status");

                    b.Property<string>("StatusDescriptionOverride");

                    b.Property<DateTime>("StatusEntryDateUtc");

                    b.HasKey("Id");

                    b.HasIndex("InitiativeId");

                    b.ToTable("InitiativeStatusHistories");
                });

            modelBuilder.Entity("CoE.Ideas.Core.Data.Stakeholder", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("InitiativeId");

                    b.Property<int>("PersonId");

                    b.Property<int>("Type");

                    b.HasKey("Id");

                    b.HasIndex("InitiativeId");

                    b.ToTable("Stakeholder");
                });

            modelBuilder.Entity("CoE.Ideas.Core.Data.StatusEta", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("EtaType");

                    b.Property<int>("Status");

                    b.Property<int>("Time");

                    b.HasKey("Id");

                    b.ToTable("StatusEtas");
                });

            modelBuilder.Entity("CoE.Ideas.Core.Data.StringTemplate", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Category");

                    b.Property<string>("Key")
                        .HasMaxLength(64);

                    b.Property<string>("Text")
                        .HasMaxLength(2048);

                    b.HasKey("Id");

                    b.ToTable("StringTemplates");
                });

            modelBuilder.Entity("CoE.Ideas.Core.Data.InitiativeStatusHistory", b =>
                {
                    b.HasOne("CoE.Ideas.Core.Data.Initiative")
                        .WithMany("StatusHistories")
                        .HasForeignKey("InitiativeId");
                });

            modelBuilder.Entity("CoE.Ideas.Core.Data.Stakeholder", b =>
                {
                    b.HasOne("CoE.Ideas.Core.Data.Initiative")
                        .WithMany("Stakeholders")
                        .HasForeignKey("InitiativeId");
                });
#pragma warning restore 612, 618
        }
    }
}
