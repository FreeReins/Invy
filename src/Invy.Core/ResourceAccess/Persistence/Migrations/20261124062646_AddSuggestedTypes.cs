using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invy.Core.ResourceAccess.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddSuggestedTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SuggestedTypeId",
                table: "Items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TypeSuggestedAt",
                table: "Items",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SuggestedTypeId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "TypeSuggestedAt",
                table: "Items");
        }
    }
}
