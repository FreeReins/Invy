using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invy.Core.ResourceAccess.Persistence.Migrations
{
    /// <summary>
    /// Spaces get feature switches, items can be examples, and members can put the getting-started card away. Spaces
    /// made before this get their kind's default features, and spaces made with no pack get none.
    /// </summary>
    internal partial class AddSpaceFeatures : Migration
    {
        /// <summary>The kinds' default features when this was released. Later pack changes don't touch old spaces.</summary>
        public static readonly IReadOnlyDictionary<string, string[]> Defaults = new Dictionary<string, string[]>
        {
            ["home"] = ["values", "barcodes", "lending", "reminders"],
            ["hobby"] = ["barcodes", "stock", "reminders", "projects"],
            ["business"] = ["values", "audits", "barcodes", "stock", "lending", "reminders"],
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "Features",
                table: "Spaces",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            foreach (var (preset, features) in Defaults)
            {
                var list = string.Join(", ", features.Select(feature => $"'{feature}'"));
                migrationBuilder.Sql($"UPDATE \"Spaces\" SET \"Features\" = ARRAY[{list}]::text[] WHERE \"Preset\" = '{preset}';");
            }

            migrationBuilder.AddColumn<bool>(
                name: "GettingStartedDismissed",
                table: "SpaceMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsExample",
                table: "Items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Items_Examples",
                table: "Items",
                column: "SpaceId",
                filter: "\"IsExample\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_Examples",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Features",
                table: "Spaces");

            migrationBuilder.DropColumn(
                name: "GettingStartedDismissed",
                table: "SpaceMembers");

            migrationBuilder.DropColumn(
                name: "IsExample",
                table: "Items");
        }
    }
}
