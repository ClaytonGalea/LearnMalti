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

        public DbSet<HangmanResult> HangmanResults { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔥 TABLE + COLUMN FIX (IMPORTANT)
            modelBuilder.Entity<LearningItem>(entity =>
            {
                entity.ToTable("learningitems");

                entity.Property(e => e.LearningItemId).HasColumnName("learningitemid");
                entity.Property(e => e.ItemType).HasColumnName("itemtype");
                entity.Property(e => e.MalteseText).HasColumnName("maltesetext");
                entity.Property(e => e.EnglishText).HasColumnName("englishtext");
                entity.Property(e => e.Category).HasColumnName("category");
                entity.Property(e => e.Difficulty).HasColumnName("difficulty");
                entity.Property(e => e.ImageUrl).HasColumnName("imageurl");
                entity.Property(e => e.WordKey).HasColumnName("wordkey");
                entity.Property(e => e.NumberForm).HasColumnName("numberform");
                entity.Property(e => e.MalteseWord_Font).HasColumnName("malteseword_font");
                entity.Property(e => e.AudioPath).HasColumnName("audiopath");
            });

            modelBuilder.Entity<Player>(entity =>
            {
                entity.ToTable("players");

                entity.Property(e => e.PlayerId).HasColumnName("playerid");
                entity.Property(e => e.PlayerCode).HasColumnName("playercode");
                entity.Property(e => e.Mode).HasColumnName("mode");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat");
                entity.Property(e => e.CurrentLevel).HasColumnName("currentlevel");
                entity.Property(e => e.CurrentXp).HasColumnName("currentxp");
            });


            modelBuilder.Entity<PlayerBadge>(entity =>
            {
                entity.ToTable("playerbadges");

                entity.Property(e => e.PlayerBadgeId).HasColumnName("playerbadgeid");
                entity.Property(e => e.PlayerId).HasColumnName("playerid");
                entity.Property(e => e.BadgeId).HasColumnName("badgeid");
                entity.Property(e => e.EarnedAt).HasColumnName("earnedat");
            });

            modelBuilder.Entity<Badge>(entity =>
            {
                entity.ToTable("badges");

                entity.Property(e => e.BadgeId).HasColumnName("badgeid");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.IconKey).HasColumnName("iconkey");
            });

            modelBuilder.Entity<LevelAttempt>(entity =>
            {
                entity.ToTable("levelattempts");

                entity.Property(e => e.LevelAttemptId).HasColumnName("levelattemptid");
                entity.Property(e => e.PlayerId).HasColumnName("playerid");
                entity.Property(e => e.LevelName).HasColumnName("levelname");
                entity.Property(e => e.Mode).HasColumnName("mode");
                entity.Property(e => e.StartedAt).HasColumnName("startedat");
                entity.Property(e => e.CompletedAt).HasColumnName("completedat");
                entity.Property(e => e.DurationSeconds).HasColumnName("durationseconds");
                entity.Property(e => e.TotalQuestions).HasColumnName("totalquestions");
                entity.Property(e => e.CorrectAnswers).HasColumnName("correctanswers");
                entity.Property(e => e.IncorrectAnswers).HasColumnName("incorrectanswers");
                entity.Property(e => e.ScorePercentage).HasColumnName("scorepercentage");
                entity.Property(e => e.TimeRanOut).HasColumnName("timeranout");
            });

            modelBuilder.Entity<TimedQuizResult>(entity =>
            {
                entity.ToTable("timedquizresults");

                entity.Property(e => e.TimedQuizResultId).HasColumnName("timedquizresultid");
                entity.Property(e => e.Mode).HasColumnName("mode");
                entity.Property(e => e.QuestionsAnswered).HasColumnName("questionsanswered");
                entity.Property(e => e.CorrectAnswers).HasColumnName("correctanswers");
                entity.Property(e => e.IncorrectAnswers).HasColumnName("incorrectanswers");
                entity.Property(e => e.PlayerCode).HasColumnName("playercode");
                entity.Property(e => e.Score).HasColumnName("score");
                entity.Property(e => e.PlayedAt).HasColumnName("playedat");
            });

            modelBuilder.Entity<HangmanResult>(entity =>
            {
                entity.ToTable("hangmanresults");

                entity.Property(e => e.HangmanResultId).HasColumnName("hangmanresultid");
                entity.Property(e => e.PlayerId).HasColumnName("playerid");
                entity.Property(e => e.Word).HasColumnName("word");
                entity.Property(e => e.WordLength).HasColumnName("wordlength");
                entity.Property(e => e.TotalGuesses).HasColumnName("totalguesses");
                entity.Property(e => e.CorrectGuesses).HasColumnName("correctguesses");
                entity.Property(e => e.WrongGuesses).HasColumnName("wrongguesses");
                entity.Property(e => e.LivesRemaining).HasColumnName("livesremaining");
                entity.Property(e => e.Completed).HasColumnName("completed");
                entity.Property(e => e.TimeTakenSeconds).HasColumnName("timetakenseconds");
                entity.Property(e => e.StartedAt).HasColumnName("startedat");
                entity.Property(e => e.CompletedAt).HasColumnName("completedat");
            });

            // Tables (others)
            modelBuilder.Entity<Player>().ToTable("players");
            modelBuilder.Entity<Badge>().ToTable("badges");
            modelBuilder.Entity<PlayerBadge>().ToTable("playerbadges");
            modelBuilder.Entity<TimedQuizResult>().ToTable("timedquizresults");
            modelBuilder.Entity<LevelAttempt>().ToTable("levelattempts");
            modelBuilder.Entity<QuestionResponse>().ToTable("questionresponses");
            modelBuilder.Entity<HangmanResult>().ToTable("hangmanresults");


            // Unique PlayerCode
            modelBuilder.Entity<Player>()
                .HasIndex(p => p.PlayerCode)
                .IsUnique();

            // Badge unique per player
            modelBuilder.Entity<PlayerBadge>()
                .HasIndex(pb => new { pb.PlayerId, pb.BadgeId })
                .IsUnique();

            // Relationships
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

            modelBuilder.Entity<QuestionResponse>()
                .HasOne(qr => qr.LearningItem)
                .WithMany()
                .HasForeignKey(qr => qr.LearningItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LevelAttempt>()
                .Property(x => x.ScorePercentage)
                .HasPrecision(5, 2);

            // Seed
            modelBuilder.Entity<Badge>().HasData(
                new Badge { BadgeId = 1, Name = "Tutorial Master", Description = "Completed the Tutorial", IconKey = "🏅" },
                new Badge { BadgeId = 2, Name = "Perfect Score", Description = "Scored 100% on any level", IconKey = "🌟" },
                new Badge { BadgeId = 3, Name = "Speed Runner", Description = "Finished a level before time ran out", IconKey = "⚡" }
            );
        }


    }
}
