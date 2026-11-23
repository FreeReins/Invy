using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invy.Core.ResourceAccess.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LabelCode",
                table: "Items",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            // The search text gains the label code (see AddSearch). A generated column's expression can't be changed
            // in place on every supported PostgreSQL, so it is dropped and added again with its index.
            migrationBuilder.Sql("""DROP INDEX "IX_Items_Search";""");
            migrationBuilder.Sql("""ALTER TABLE "Items" DROP COLUMN "Search";""");
            migrationBuilder.Sql("""
                ALTER TABLE "Items" ADD COLUMN "Search" tsvector GENERATED ALWAYS AS (
                    setweight(to_tsvector('simple', coalesce("Name", '')), 'A')
                    || setweight(to_tsvector('simple', coalesce("Marking", '')), 'A')
                    || setweight(to_tsvector('simple', coalesce("LabelCode", '')), 'A')
                    || setweight(to_tsvector('simple', invy_join_words("Tags")), 'B')
                    || setweight(jsonb_to_tsvector('simple', "Fields", '["string"]'), 'C')
                    || setweight(to_tsvector('simple', coalesce("Notes", '')), 'D')
                ) STORED;
                """);
            migrationBuilder.Sql("""CREATE INDEX "IX_Items_Search" ON "Items" USING gin ("Search");""");

            migrationBuilder.CreateTable(
                name: "Labels",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PrintedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Labels", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Labels_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Labels_Spaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "Spaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_LabelCode",
                table: "Items",
                column: "LabelCode",
                unique: true,
                filter: "\"LabelCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Labels_ItemId",
                table: "Labels",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Labels_SpaceId_Code",
                table: "Labels",
                columns: new[] { "SpaceId", "Code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Labels");

            migrationBuilder.DropIndex(
                name: "IX_Items_LabelCode",
                table: "Items");

            // The search text loses the label code again (see AddSearch). A generated column's expression can't be changed
            // in place on every supported PostgreSQL, so it is dropped and added again with its index.
            migrationBuilder.Sql("""DROP INDEX "IX_Items_Search";""");
            migrationBuilder.Sql("""ALTER TABLE "Items" DROP COLUMN "Search";""");
            migrationBuilder.Sql("""
                ALTER TABLE "Items" ADD COLUMN "Search" tsvector GENERATED ALWAYS AS (
                    setweight(to_tsvector('simple', coalesce("Name", '')), 'A')
                    || setweight(to_tsvector('simple', coalesce("Marking", '')), 'A')
                    || setweight(to_tsvector('simple', invy_join_words("Tags")), 'B')
                    || setweight(jsonb_to_tsvector('simple', "Fields", '["string"]'), 'C')
                    || setweight(to_tsvector('simple', coalesce("Notes", '')), 'D')
                ) STORED;
                """);
            migrationBuilder.Sql("""CREATE INDEX "IX_Items_Search" ON "Items" USING gin ("Search");""");

            migrationBuilder.DropColumn(
                name: "LabelCode",
                table: "Items");
        }
    }
}
