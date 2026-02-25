using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnMalti.Migrations
{
    /// <inheritdoc />
    public partial class AddedIncorrectAnswersToTimedQuizResukts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IncorrectAnswers",
                table: "TimedQuizResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "ScorePercentage",
                table: "LevelAttempts",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncorrectAnswers",
                table: "TimedQuizResults");

            migrationBuilder.AlterColumn<decimal>(
                name: "ScorePercentage",
                table: "LevelAttempts",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);
        }
    }
}
