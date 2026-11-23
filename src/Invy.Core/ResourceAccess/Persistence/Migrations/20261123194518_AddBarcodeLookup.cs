using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invy.Core.ResourceAccess.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddBarcodeLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BarcodeLookup",
                table: "SecuritySettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BarcodeLookup",
                table: "SecuritySettings");
        }
    }
}
