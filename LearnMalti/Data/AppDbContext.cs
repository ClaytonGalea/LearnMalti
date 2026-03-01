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
        public DbSet<Badge> Badges { get; set; }
        public DbSet<PlayerBadge> PlayerBadges { get; set; }
        public DbSet<TimedQuizResult> TimedQuizResults { get; set; }

        public DbSet<LevelAttempt> LevelAttempts { get; set; }
        public DbSet<QuestionResponse> QuestionResponses { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Unique PlayerCode
            modelBuilder.Entity<Player>()
                .HasIndex(p => p.PlayerCode)
                .IsUnique();

            // Composite unique index: one progress row per (Player, Item)
           // modelBuilder.Entity<PlayerProgress>()
             //   .HasIndex(pp => new { pp.PlayerId, pp.LearningItemId })
               // .IsUnique();

            // Composite unique index: badge can't be earned twice
            modelBuilder.Entity<PlayerBadge>()
                .HasIndex(pb => new { pb.PlayerId, pb.BadgeId })
                .IsUnique();

            // LevelAttempt → Player
            modelBuilder.Entity<LevelAttempt>()
                .HasOne(la => la.Player)
                .WithMany()
                .HasForeignKey(la => la.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuestionResponse>()
                 .HasOne(qr => qr.LevelAttempt)
                 .WithMany(la => la.QuestionResponses)
                 .HasForeignKey(qr => qr.LevelAttemptId)
                 .OnDelete(DeleteBehavior.Cascade);


            // QuestionResponse → LearningItem
            modelBuilder.Entity<QuestionResponse>()
                .HasOne(qr => qr.LearningItem)
                .WithMany()
                .HasForeignKey(qr => qr.LearningItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LevelAttempt>()
            .Property(x => x.ScorePercentage)
            .HasPrecision(5, 2);


            modelBuilder.Entity<Badge>().HasData(
             new Badge { BadgeId = 1, Name = "Tutorial Master", Description = "Completed the Tutorial", IconKey = "🏅" },
             new Badge { BadgeId = 2, Name = "Perfect Score", Description = "Scored 100% on any level", IconKey = "🌟" },
             new Badge { BadgeId = 3, Name = "Speed Runner", Description = "Finished a level before time ran out", IconKey = "⚡" }
    );
        }


    }
}
