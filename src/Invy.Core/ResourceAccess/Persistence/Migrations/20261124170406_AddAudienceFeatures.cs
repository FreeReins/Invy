using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invy.Core.ResourceAccess.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddAudienceFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CalendarToken",
                table: "Spaces",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DestinationId",
                table: "Items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DueOn",
                table: "Items",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LentAt",
                table: "Items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LentTo",
                table: "Items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LowStockAt",
                table: "Items",
                type: "numeric(12,3)",
                precision: 12,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Reserved",
                table: "Items",
                type: "numeric(12,3)",
                precision: 12,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Imports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: true),
                    Mapping = table.Column<string>(type: "jsonb", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    TypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Total = table.Column<int>(type: "integer", nullable: false),
                    Done = table.Column<int>(type: "integer", nullable: false),
                    Added = table.Column<int>(type: "integer", nullable: false),
                    Problems = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Imports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Imports_Spaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "Spaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Spaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "Spaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectItems",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectItems", x => new { x.ProjectId, x.ItemId });
                    table.ForeignKey(
                        name: "FK_ProjectItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectItems_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_Destination",
                table: "Items",
                columns: new[] { "DestinationId", "SpaceId" },
                filter: "\"DestinationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Lent",
                table: "Items",
                columns: new[] { "SpaceId", "DueOn" },
                filter: "\"LentAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Items_LowStock",
                table: "Items",
                column: "SpaceId",
                filter: "\"LowStockAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Imports_SpaceId_CreatedAt",
                table: "Imports",
                columns: new[] { "SpaceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectItems_ItemId",
                table: "ProjectItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_SpaceId_Status_Name",
                table: "Projects",
                columns: new[] { "SpaceId", "Status", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Items_DestinationId",
                table: "Items",
                column: "DestinationId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Items_DestinationId",
                table: "Items");

            migrationBuilder.DropTable(
                name: "Imports");

            migrationBuilder.DropTable(
                name: "ProjectItems");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Items_Destination",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_Lent",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_LowStock",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "CalendarToken",
                table: "Spaces");

            migrationBuilder.DropColumn(
                name: "DestinationId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DueOn",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "LentAt",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "LentTo",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "LowStockAt",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Reserved",
                table: "Items");
        }
    }
}
