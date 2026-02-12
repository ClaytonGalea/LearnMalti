using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnMalti.Migrations
{
    /// <inheritdoc />
    public partial class IgnoreNumberForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tense",
                table: "LearningItems");

            migrationBuilder.DropColumn(
                name: "VerbKey",
                table: "LearningItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tense",
                table: "LearningItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerbKey",
                table: "LearningItems",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
