using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LearnMalti.Migrations
{
    /// <inheritdoc />
    public partial class RemovedRetyCountColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "BadgeId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "BadgeId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "BadgeId",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "LevelAttempts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "LevelAttempts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "Badges",
                columns: new[] { "BadgeId", "Description", "IconKey", "Name" },
                values: new object[,]
                {
                    { 1, "Completed the Tutorial", "🏅", "Tutorial Master" },
                    { 2, "Scored 100% on any level", "🌟", "Perfect Score" },
                    { 3, "Finished a level before time ran out", "⚡", "Speed Runner" }
                });
        }
    }
}
