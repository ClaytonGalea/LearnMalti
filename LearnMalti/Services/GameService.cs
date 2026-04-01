using LearnMalti.Data;
using LearnMalti.Models;

namespace LearnMalti.Services
{
    public class GameService
    {

        private readonly AppDbContext _context;

        public GameService(AppDbContext context)
        {
            _context = context;
        }

        // Award Badge
        public void AwardBadgeIfNotExists(string playerCode, int badgeId)
        {
            var player = _context.Players
                .FirstOrDefault(p => p.PlayerCode == playerCode);

            if (player == null)
                return;

            bool alreadyHasBadge = _context.PlayerBadges
                .Any(pb => pb.PlayerId == player.PlayerId && pb.BadgeId == badgeId);

            if (!alreadyHasBadge)
            {
                var playerBadge = new PlayerBadge
                {
                    PlayerId = player.PlayerId,
                    BadgeId = badgeId,
                    EarnedAt = DateTime.UtcNow
                };

                _context.PlayerBadges.Add(playerBadge);
                _context.SaveChanges();
            }
        }

        // Start Level Attempt
        public void EnsureAttemptStarted(string playerCode, string levelName, int mode, int totalQuestions, int step, HttpContext httpContext)
        {
            if (step != 1 || httpContext.Session.GetInt32("CurrentAttemptId") != null)
                return;

            var player = _context.Players.FirstOrDefault(p => p.PlayerCode == playerCode);
            if (player == null) return;

            var attempt = new LevelAttempt
            {
                PlayerId = player.PlayerId,
                LevelName = levelName,
                Mode = mode,
                StartedAt = DateTime.UtcNow,
                TotalQuestions = totalQuestions
            };

            _context.LevelAttempts.Add(attempt);
            _context.SaveChanges();

            httpContext.Session.SetInt32("CurrentAttemptId", attempt.LevelAttemptId);
        }

        // Update Attempt
        public void UpdateAttemptStats(bool isCorrect, HttpContext httpContext)
        {
            var attemptId = httpContext.Session.GetInt32("CurrentAttemptId");

            if (!attemptId.HasValue) return;

            var attempt = _context.LevelAttempts
                .FirstOrDefault(a => a.LevelAttemptId == attemptId.Value);

            if (attempt == null) return;

            if (isCorrect)
                attempt.CorrectAnswers++;
            else
                attempt.IncorrectAnswers++;

            _context.SaveChanges();
        }

        // Finish Attempt
        public void FinishAttempt(bool timeUp, HttpContext httpContext)
        {
            var attemptId = httpContext.Session.GetInt32("CurrentAttemptId");

            if (!attemptId.HasValue) return;

            var attempt = _context.LevelAttempts
                .FirstOrDefault(a => a.LevelAttemptId == attemptId.Value);

            if (attempt == null || attempt.CompletedAt != null) return;

            attempt.CompletedAt = DateTime.UtcNow;

            attempt.DurationSeconds =
                (int)(attempt.CompletedAt.Value - attempt.StartedAt).TotalSeconds;

            attempt.TimeRanOut = timeUp;

            attempt.ScorePercentage =
                attempt.TotalQuestions > 0
                ? Math.Round((decimal)attempt.CorrectAnswers / attempt.TotalQuestions * 100, 2)
                : 0;

            _context.SaveChanges();
        }
    }
}
