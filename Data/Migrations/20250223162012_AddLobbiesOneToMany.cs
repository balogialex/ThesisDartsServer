using Microsoft.EntityFrameworkCore.Migrations;

namespace DartsAPI.Data.Migrations
{
    public partial class AddLobbiesOneToMany : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove existing players if they exist (optional, depends on your data)
            migrationBuilder.DeleteData(
                table: "Players",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Players",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Players",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Players",
                keyColumn: "Id",
                keyValue: 4);

            // Add LobbyId column to Players
            migrationBuilder.AddColumn<int>(
                name: "LobbyId",
                table: "Players",
                type: "INTEGER",
                nullable: true);

            // Create Lobbies table with correct CreatorId
            migrationBuilder.CreateTable(
                name: "Lobbies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatorId = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxPlayers = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lobbies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lobbies_Players_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create index for Players.LobbyId
            migrationBuilder.CreateIndex(
                name: "IX_Players_LobbyId",
                table: "Players",
                column: "LobbyId");

            // Create index for Lobbies.CreatorId
            migrationBuilder.CreateIndex(
                name: "IX_Lobbies_CreatorId",
                table: "Lobbies",
                column: "CreatorId");

            // Add foreign key for Players.LobbyId
            migrationBuilder.AddForeignKey(
                name: "FK_Players_Lobbies_LobbyId",
                table: "Players",
                column: "LobbyId",
                principalTable: "Lobbies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull); // Optional: SetNull instead of Restrict
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Lobbies_LobbyId",
                table: "Players");

            migrationBuilder.DropTable(
                name: "Lobbies");

            migrationBuilder.DropIndex(
                name: "IX_Players_LobbyId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "LobbyId",
                table: "Players");

            // Restore player data if needed
            migrationBuilder.InsertData(
                table: "Players",
                columns: new[] { "Id", "PasswordHash", "Username" }, // Adjusted column name
                values: new object[,]
                {
                    { 1, "asd", "Alex" },
                    { 2, "asd", "Marci" },
                    { 3, "asd", "Ármin" },
                    { 4, "asd", "Kero" }
                });
        }
    }
}