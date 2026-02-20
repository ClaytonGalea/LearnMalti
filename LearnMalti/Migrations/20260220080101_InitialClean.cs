using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LearnMalti.Migrations
{
    /// <inheritdoc />
    public partial class InitialClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Badges",
                columns: table => new
                {
                    BadgeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IconKey = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Badges", x => x.BadgeId);
                });

            migrationBuilder.CreateTable(
                name: "LearningItems",
                columns: table => new
                {
                    LearningItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MalteseText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EnglishText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WordKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberForm = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningItems", x => x.LearningItemId);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    YearGroup = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentLevel = table.Column<int>(type: "int", nullable: false),
                    CurrentXp = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.PlayerId);
                });

            migrationBuilder.CreateTable(
                name: "TimedQuizResults",
                columns: table => new
                {
                    TimedQuizResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    QuestionsAnswered = table.Column<int>(type: "int", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "int", nullable: false),
                    PlayerCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    PlayedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimedQuizResults", x => x.TimedQuizResultId);
                });

            migrationBuilder.CreateTable(
                name: "LevelAttempts",
                columns: table => new
                {
                    LevelAttemptId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    LevelName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "int", nullable: false),
                    IncorrectAnswers = table.Column<int>(type: "int", nullable: false),
                    ScorePercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TimeRanOut = table.Column<bool>(type: "bit", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LevelAttempts", x => x.LevelAttemptId);
                    table.ForeignKey(
                        name: "FK_LevelAttempts_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerBadges",
                columns: table => new
                {
                    PlayerBadgeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    BadgeId = table.Column<int>(type: "int", nullable: false),
                    EarnedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerBadges", x => x.PlayerBadgeId);
                    table.ForeignKey(
                        name: "FK_PlayerBadges_Badges_BadgeId",
                        column: x => x.BadgeId,
                        principalTable: "Badges",
                        principalColumn: "BadgeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerBadges_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    LearningItemId = table.Column<int>(type: "int", nullable: false),
                    TotalAttempts = table.Column<int>(type: "int", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "int", nullable: false),
                    WasLastAnswerCorrect = table.Column<bool>(type: "bit", nullable: false),
                    LastAttemptTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerProgress_LearningItems_LearningItemId",
                        column: x => x.LearningItemId,
                        principalTable: "LearningItems",
                        principalColumn: "LearningItemId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerProgress_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionResponses",
                columns: table => new
                {
                    QuestionResponseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LevelAttemptId = table.Column<int>(type: "int", nullable: false),
                    LearningItemId = table.Column<int>(type: "int", nullable: false),
                    SelectedAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    TimeTakenSeconds = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionResponses", x => x.QuestionResponseId);
                    table.ForeignKey(
                        name: "FK_QuestionResponses_LearningItems_LearningItemId",
                        column: x => x.LearningItemId,
                        principalTable: "LearningItems",
                        principalColumn: "LearningItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionResponses_LevelAttempts_LevelAttemptId",
                        column: x => x.LevelAttemptId,
                        principalTable: "LevelAttempts",
                        principalColumn: "LevelAttemptId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Badges",
                columns: new[] { "BadgeId", "Description", "IconKey", "Name" },
                values: new object[,]
                {
                    { 1, "Completed the Tutorial", "🏅", "Tutorial Master" },
                    { 2, "Scored 100% on any level", "🌟", "Perfect Score" },
                    { 3, "Finished a level before time ran out", "⚡", "Speed Runner" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LevelAttempts_PlayerId",
                table: "LevelAttempts",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBadges_BadgeId",
                table: "PlayerBadges",
                column: "BadgeId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBadges_PlayerId_BadgeId",
                table: "PlayerBadges",
                columns: new[] { "PlayerId", "BadgeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerProgress_LearningItemId",
                table: "PlayerProgress",
                column: "LearningItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerProgress_PlayerId_LearningItemId",
                table: "PlayerProgress",
                columns: new[] { "PlayerId", "LearningItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_PlayerCode",
                table: "Players",
                column: "PlayerCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionResponses_LearningItemId",
                table: "QuestionResponses",
                column: "LearningItemId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionResponses_LevelAttemptId",
                table: "QuestionResponses",
                column: "LevelAttemptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerBadges");

            migrationBuilder.DropTable(
                name: "PlayerProgress");

            migrationBuilder.DropTable(
                name: "QuestionResponses");

            migrationBuilder.DropTable(
                name: "TimedQuizResults");

            migrationBuilder.DropTable(
                name: "Badges");

            migrationBuilder.DropTable(
                name: "LearningItems");

            migrationBuilder.DropTable(
                name: "LevelAttempts");

            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}
