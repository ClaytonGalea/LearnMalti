using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnMalti.Migrations
{
    /// <inheritdoc />
    public partial class AddWordKeyToLearningItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Column already exists in database.
            // Migration intentionally left empty.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op
        }
    }
}
