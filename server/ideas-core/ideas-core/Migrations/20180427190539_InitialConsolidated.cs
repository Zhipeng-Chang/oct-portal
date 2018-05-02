﻿using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace CoE.Ideas.Core.Migrations
{
    public partial class InitialConsolidated : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Initiatives",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ApexId = table.Column<int>(nullable: true),
                    AssigneeId = table.Column<int>(nullable: true),
                    BusinessCaseUrl = table.Column<string>(maxLength: 2048, nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(nullable: false),
                    Description = table.Column<string>(nullable: false),
                    InvestmentRequestFormUrl = table.Column<string>(maxLength: 2048, nullable: true),
                    Status = table.Column<int>(nullable: false),
                    Title = table.Column<string>(maxLength: 255, nullable: false),
                    Uid = table.Column<Guid>(nullable: false),
                    WorkOrderId = table.Column<string>(maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Initiatives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatusEtas",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    EtaType = table.Column<int>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    Time = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusEtas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StringTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Category = table.Column<int>(nullable: false),
                    Key = table.Column<string>(maxLength: 64, nullable: true),
                    Text = table.Column<string>(maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StringTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InitiativeStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ExpectedExitDateUtc = table.Column<DateTime>(nullable: true),
                    InitiativeId = table.Column<int>(nullable: true),
                    PersonId = table.Column<int>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    StatusDescriptionOverride = table.Column<string>(nullable: true),
                    StatusEntryDateUtc = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InitiativeStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InitiativeStatusHistories_Initiatives_InitiativeId",
                        column: x => x.InitiativeId,
                        principalTable: "Initiatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stakeholder",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    InitiativeId = table.Column<int>(nullable: true),
                    PersonId = table.Column<int>(nullable: false),
                    Type = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stakeholder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stakeholder_Initiatives_InitiativeId",
                        column: x => x.InitiativeId,
                        principalTable: "Initiatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Initiatives_WorkOrderId",
                table: "Initiatives",
                column: "WorkOrderId",
                unique: true,
                filter: "[WorkOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeStatusHistories_InitiativeId",
                table: "InitiativeStatusHistories",
                column: "InitiativeId");

            migrationBuilder.CreateIndex(
                name: "IX_Stakeholder_InitiativeId",
                table: "Stakeholder",
                column: "InitiativeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.DropTable(
                name: "InitiativeStatusHistories");

            migrationBuilder.DropTable(
                name: "Stakeholder");

            migrationBuilder.DropTable(
                name: "StatusEtas");

            migrationBuilder.DropTable(
                name: "StringTemplates");

            migrationBuilder.DropTable(
                name: "Initiatives");
        }
    }
}
