using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invy.Core.ResourceAccess.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddItemPaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 7: paths move to their own narrow table, so moving a container rewrites only these rows.
            migrationBuilder.CreateTable(
                name: "ItemPaths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "ltree", nullable: false),
                    Depth = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemPaths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemPaths_Items_Id",
                        column: x => x.Id,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""INSERT INTO "ItemPaths" ("Id", "Path", "Depth") SELECT "Id", "Path", "Depth" FROM "Items";""");

            migrationBuilder.CreateIndex(
                name: "IX_ItemPaths_Path",
                table: "ItemPaths",
                column: "Path")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.DropIndex(
                name: "IX_Items_Path",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Path",
                table: "Items");

            // Search's in: finds its containers by name, marking or label code, ignoring case (SearchSql.InsideSql).
            migrationBuilder.Sql("""CREATE INDEX "IX_Items_ContainerName" ON "Items" ("SpaceId", lower("Name")) WHERE "HoldsItems";""");
            migrationBuilder.Sql("""CREATE INDEX "IX_Items_ContainerMarking" ON "Items" ("SpaceId", lower("Marking")) WHERE "HoldsItems" AND "Marking" IS NOT NULL;""");
            migrationBuilder.Sql("""CREATE INDEX "IX_Items_ContainerLabel" ON "Items" ("SpaceId", lower("LabelCode")) WHERE "HoldsItems" AND "LabelCode" IS NOT NULL;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX "IX_Items_ContainerName", "IX_Items_ContainerMarking", "IX_Items_ContainerLabel";""");

            migrationBuilder.AddColumn<int>(
                name: "Depth",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "Items",
                type: "ltree",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""UPDATE "Items" i SET "Path" = p."Path", "Depth" = p."Depth" FROM "ItemPaths" p WHERE p."Id" = i."Id";""");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Path",
                table: "Items",
                column: "Path")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.DropTable(
                name: "ItemPaths");
        }
    }
}
