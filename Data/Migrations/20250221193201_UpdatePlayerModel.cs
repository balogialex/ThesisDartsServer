using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartsAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlayerModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Players",
                newName: "Username");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Username",
                table: "Players",
                newName: "Name");
        }
    }
}
