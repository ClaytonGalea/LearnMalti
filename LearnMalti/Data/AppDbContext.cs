using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Numerics;
using LearnMalti.Models;
namespace LearnMalti.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
        {
        }

        // Tables
        public DbSet<Player> Players { get; set; }
        public DbSet<LearningItem> LearningItems { get; set; }
        public DbSet<PlayerProgress> PlayerProgress { get; set; }
        public DbSet<Assessment> Assessments { get; set; }
        public DbSet<AssessmentResult> AssessmentResults { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<PlayerBadge> PlayerBadges { get; set; }
        public DbSet<SurveyQuestion> SurveyQuestions { get; set; }
        public DbSet<SurveyResponse> SurveyResponses { get; set; }

        public DbSet<TimedQuizResult> TimedQuizResults { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Unique PlayerCode
            modelBuilder.Entity<Player>()
                .HasIndex(p => p.PlayerCode)
                .IsUnique();

            // Composite unique index: one progress row per (Player, Item)
            modelBuilder.Entity<PlayerProgress>()
                .HasIndex(pp => new { pp.PlayerId, pp.LearningItemId })
                .IsUnique();

            // Composite unique index: badge can't be earned twice
            modelBuilder.Entity<PlayerBadge>()
                .HasIndex(pb => new { pb.PlayerId, pb.BadgeId })
                .IsUnique();

            modelBuilder.Entity<Badge>().HasData(
            new Badge { BadgeId = 1, Name = "Tutorial Master", Description = "Completed the Tutorial", IconKey = "🏅" },
            new Badge { BadgeId = 2, Name = "Perfect Score", Description = "Scored 100% on any level", IconKey = "🌟" },
            new Badge { BadgeId = 3, Name = "Speed Runner", Description = "Finished a level before time ran out", IconKey = "⚡" }
    );
        }


    }
}
