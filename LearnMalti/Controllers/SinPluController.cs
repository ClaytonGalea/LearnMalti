using LearnMalti.Data;
using LearnMalti.Models;
using Microsoft.AspNetCore.Mvc;

namespace LearnMalti.Controllers
{
    public class SinPluController : Controller
    {
        private readonly AppDbContext _context;

        private const string CategoryName = "SinPlu";
        private const string LevelName = "SinPlu";
        private const int BadgeId = 12;
        private const int TotalPairs = 8;
        private const int Lives = 3;

        public SinPluController(AppDbContext context)
        {
            _context = context;
        }

        private class SinPluPair
        {
            public string WordKey { get; set; }
            public LearningItem Singular { get; set; }
            public LearningItem Plural { get; set; }
        }

        public IActionResult Start(string playerCode, int step = 1, int mode = 1, int lives = 3)
        {
            var pairs = GetPairs(playerCode);
    
            EnsureAttemptStarted(playerCode, mode, pairs.Count, step);

            if (lives <= 0)
            {
                return RedirectToAction("Completed", new
                {
                    playerCode,
                    total = pairs.Count,
                    mode,
                    failed = true
                });
            }

            if (step < 1 || step > pairs.Count)
            {
                return RedirectToAction("Completed", new
                {
                    playerCode,
                    total = pairs.Count,
                    mode
                });
            }

            var current = pairs[step - 1];
            var questionType = GetQuestionType(step);

            ViewBag.QuestionType = questionType;
            ViewBag.Step = step;
            ViewBag.TotalSteps = pairs.Count;
            ViewBag.PlayerCode = playerCode;
            ViewBag.Mode = mode;
            ViewBag.Lives = lives;

            ViewBag.WordKey = current.WordKey;
            ViewBag.Singular = current.Singular;
            ViewBag.Plural = current.Plural;

            ViewData["Title"] = "SinPlu Level";

           GenerateChoices(pairs, current, questionType);

            return View("Start");
        }

        public IActionResult SubmitAnswer(
           string playerCode,
           int step,
           int mode,
           int lives,
           int learningItemId,
           bool isCorrect)
        {
           UpdateAttemptStats(isCorrect);

            return RedirectToAction("Start", new
            {
                playerCode,
                step = step + 1,
                mode,
                lives
            });
        }

        public IActionResult Completed(string playerCode, int mode, bool timeUp = false, bool failed = false)
        {
           FinishAttempt(timeUp);

            HttpContext.Session.Remove("CurrentAttemptId");

            ViewBag.PlayerCode = playerCode;
            ViewBag.Mode = mode;
            ViewBag.TimeUp = timeUp;
            ViewBag.Failed = failed;

            if (!timeUp && !failed && mode == 1)
            {
                AwardBadgeIfNotExists(playerCode, BadgeId);
            }

            ViewBag.Layout = "~/Views/Shared/_SinPluLayout.cshtml";
            ViewBag.Title = "Singular & Plural Completed";
            ViewBag.BadgeText = GetCompletionText(mode, timeUp, failed);
            ViewBag.RetryUrl = $"/SinPlu/Start?playerCode={playerCode}&step=1&score=0&mode={mode}&lives=3";
            ViewBag.ShowFeedback = false;

            return View("~/Views/Game/Complete.cshtml");
        }

        private List<SinPluPair> GetPairs(string playerCode)
        {
            var items = _context.LearningItems
                .Where(x => x.Category == CategoryName)
                .ToList();

            var allPairs = items
                .Where(x => x.WordKey != null && x.NumberForm != null)
                .GroupBy(x => x.WordKey)
                .Select(g => new SinPluPair
                {
                    WordKey = g.Key,
                    Singular = g.First(x => x.NumberForm.ToLower() == "singular"),
                    Plural = g.First(x => x.NumberForm.ToLower() == "plural")
                })
                .ToList();

            var rng = new Random(playerCode.GetHashCode());

            return allPairs
                .OrderBy(x => rng.Next())
                .Take(TotalPairs)
                .ToList();
        }

        private void GenerateChoices(List<SinPluPair> pairs, SinPluPair current, string type)
        {
            if (type == "Quiz")
            {
                ViewBag.CorrectAnswer = current.Plural.DisplayMalteseWord;

                var choices = new List<string>
                {
                    current.Singular.DisplayMalteseWord,
                    current.Plural.DisplayMalteseWord,
                    GetThirdOption(current.WordKey)
                };

                ViewBag.Choices = choices
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => Guid.NewGuid())
                    .ToList();
            }

            if (type == "Input")
            {
                ViewBag.Prompt = "Type the correct plural word";
                ViewBag.CorrectAnswer = current.Plural.DisplayMalteseWord;
            }

            
        }

        private string GetQuestionType(int step)
        {
            if (step <= 4) return "Quiz";
            if (step <= 6) return "Input";
            return "Matching";
        }


        private string GetThirdOption(string wordKey)
        {
            return wordKey switch
            {
                "cup" => "Tazziet",
                "flower" => "Fjuriet",
                "cow" => "Baqriet",
                "horse" => "Żwimel",
                "box" => "Kaxxijiet",
                "motorcycle" => "Muturs",
                "paper" => "Kartijiet",
                "police" => "Pulizijiet",
                _ => "—"
            };
        }

        private void AwardBadgeIfNotExists(string playerCode, int badgeId)
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
                    EarnedAt = DateTime.Now
                };

                _context.PlayerBadges.Add(playerBadge);
                _context.SaveChanges();
            }
        }
     

        private string GetCompletionText(int mode, bool timeUp, bool failed)
        {
            if (timeUp)
                return "Unlucky! You ran out of time.";

            if (failed)
                return "You used all your lives. Give it another try!";

            return mode == 1
                ? "Great job! You earned the Grammar Badge!"
                : "Great job!";
        }




        private void EnsureAttemptStarted(string playerCode, int mode, int totalQuestions, int step)
        {
            if (step != 1 || HttpContext.Session.GetInt32("CurrentAttemptId") != null)
                return;

            var player = _context.Players.FirstOrDefault(p => p.PlayerCode == playerCode);
            if (player == null) return;

            var attempt = new LevelAttempt
            {
                PlayerId = player.PlayerId,
                LevelName = LevelName,
                Mode = mode,
                StartedAt = DateTime.UtcNow,
                TotalQuestions = totalQuestions
            };

            _context.LevelAttempts.Add(attempt);
            _context.SaveChanges();

            HttpContext.Session.SetInt32("CurrentAttemptId", attempt.LevelAttemptId);
        }

        private void UpdateAttemptStats(bool isCorrect)
        {
            var attemptId = HttpContext.Session.GetInt32("CurrentAttemptId");

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



        private void FinishAttempt(bool timeUp)
        {
            var attemptId = HttpContext.Session.GetInt32("CurrentAttemptId");

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


        public IActionResult Index()
        {
            return View();
        }
    }
}
