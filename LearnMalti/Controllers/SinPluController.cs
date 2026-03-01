using LearnMalti.Data;
using LearnMalti.Models;
using Microsoft.AspNetCore.Mvc;

namespace LearnMalti.Controllers
{
    public class SinPluController : Controller
    {
        private readonly AppDbContext _context;

        public SinPluController(AppDbContext context)
        {
            _context = context;
        }

        // 🔒 Helper DTO (controller-only)
        private class SinPluPair
        {
            public string WordKey { get; set; }
            public LearningItem Singular { get; set; }
            public LearningItem Plural { get; set; }
        }

        public IActionResult Start(string playerCode, int step = 1, int score = 0, int mode = 1, int lives = 3)
        {
            var items = _context.LearningItems
                .Where(x => x.Category == "SinPlu")
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


            // 🔒 stable shuffle per player
            var rng = new Random(playerCode.GetHashCode());

            var pairs = allPairs
                .OrderBy(x => rng.Next())
                .Take(8)
                .ToList();

            if (step == 1 && HttpContext.Session.GetInt32("CurrentAttemptId") == null)
            {
                var player = _context.Players
                    .FirstOrDefault(p => p.PlayerCode == playerCode);

                if (player != null)
                {
                    var attempt = new LevelAttempt
                    {
                        PlayerId = player.PlayerId,
                        LevelName = "SinPlu",
                        Mode = mode,
                        StartedAt = DateTime.UtcNow,
                        TotalQuestions = pairs.Count,
                        CorrectAnswers = 0,
                        IncorrectAnswers = 0,
                        RetryCount = 0,
                        TimeRanOut = false
                    };

                    _context.LevelAttempts.Add(attempt);
                    _context.SaveChanges();

                    HttpContext.Session.SetInt32("CurrentAttemptId", attempt.LevelAttemptId);
                }
            }

            // ❌ Failed
            if (lives <= 0)
            {
                return RedirectToAction("Completed", new
                {
                    playerCode,
                    score,
                    total = pairs.Count,
                    mode,
                    failed = true
                });
            }

            // ✅ Finished
            if (step < 1 || step > pairs.Count)
            {
                return RedirectToAction("Completed", new
                {
                    playerCode,
                    score,
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
            ViewBag.Score = score;
            ViewBag.Mode = mode;
            ViewBag.Lives = lives;

            ViewBag.WordKey = current.WordKey;
            ViewBag.Singular = current.Singular;
            ViewBag.Plural = current.Plural;

            // 🟦 QUIZ (1–5)
            if (questionType == "Quiz")
            {
                ViewBag.CorrectAnswer = current.Plural.DisplayMalteseWord;

                var choices = new List<string>
                {
                    current.Singular.DisplayMalteseWord,          // ❌ singular
                    current.Plural.DisplayMalteseWord,            // ✅ plural
                    GetThirdOption(current.WordKey)         // ❌ custom
                };

                // 🔒 Remove accidental duplicates
                choices = choices
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                ViewBag.Choices = choices
                    .OrderBy(x => Guid.NewGuid())
                    .ToList();
            }

            // 🟨 INPUT (6–7)
            if (questionType == "Input")
            {
                ViewBag.Prompt = "Type the correct word for the picture";
                ViewBag.CorrectAnswer = current.Plural.DisplayMalteseWord;
            }

            // 🟥 MATCHING (8)
            if (questionType == "Matching")
            {
                SetupMatching(pairs);

                ViewBag.CorrectPairsJson =
                  System.Text.Json.JsonSerializer.Serialize(ViewBag.CorrectPairs);
            }

            return View("Start");
        }

        public IActionResult SubmitAnswer(
   string playerCode,
   int step,
   int score,
   int mode,
   int lives,
   int learningItemId,
   bool isCorrect)
        {
            var attemptId = HttpContext.Session.GetInt32("CurrentAttemptId");

            if (!attemptId.HasValue)
                return RedirectToAction("Start", new { playerCode, step, score, mode, lives });

            var attempt = _context.LevelAttempts
                .FirstOrDefault(a => a.LevelAttemptId == attemptId.Value);

            if (attempt == null)
                return RedirectToAction("Start", new { playerCode, step, score, mode, lives });

            if (isCorrect)
                attempt.CorrectAnswers++;
            else
                attempt.IncorrectAnswers++;

            _context.SaveChanges();

            return RedirectToAction("Start", new
            {
                playerCode,
                step = isCorrect ? step + 1 : step,
                score,
                mode,
                lives
            });
        }
        private void AwardBadgeIfNotExists(string playerCode, int badgeId)
        {
            // 1️⃣ Find the player
            var player = _context.Players
                .FirstOrDefault(p => p.PlayerCode == playerCode);

            if (player == null)
                return; // safety check

            // 2️⃣ Check if badge already exists
            bool alreadyHasBadge = _context.PlayerBadges
                .Any(pb => pb.PlayerId == player.PlayerId && pb.BadgeId == badgeId);

            if (!alreadyHasBadge)
            {
                var playerBadge = new PlayerBadge
                {
                    PlayerId = player.PlayerId, // ✅ FK
                    BadgeId = badgeId,
                    EarnedAt = DateTime.Now
                };

                _context.PlayerBadges.Add(playerBadge);
                _context.SaveChanges();
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

        private void SetupMatching(List<SinPluPair> pairs)
        {
            var selected = pairs
                .OrderBy(x => Guid.NewGuid())
                .Take(5)
                .ToList();

            ViewBag.LeftSide = selected
                .Select(x => x.Singular.MalteseText)
                .OrderBy(x => Guid.NewGuid())
                .ToList();

            ViewBag.RightSide = selected
                .Select(x => x.Plural.MalteseText)
                .OrderBy(x => Guid.NewGuid())
                .ToList();

            ViewBag.CorrectPairs = selected
                .Select(x => new
                {
                    Singular = x.Singular.MalteseText,
                    Plural = x.Plural.MalteseText
                })
                .ToList();
        }

        public IActionResult Completed(string playerCode, int mode, bool timeUp = false, bool failed = false)
        {
            var attemptId = HttpContext.Session.GetInt32("CurrentAttemptId");

            if (attemptId.HasValue)
            {
                var attempt = _context.LevelAttempts
                    .FirstOrDefault(a => a.LevelAttemptId == attemptId.Value);

                if (attempt != null && attempt.CompletedAt == null)
                {
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

            HttpContext.Session.Remove("CurrentAttemptId");
            ViewBag.PlayerCode = playerCode;
            ViewBag.Mode = mode;
            ViewBag.TimeUp = timeUp;
            ViewBag.Failed = failed;

            if (!timeUp && !failed && mode == 1)
            {
                AwardBadgeIfNotExists(playerCode, 12);
            }

            ViewBag.Layout = "~/Views/Shared/_SinPluLayout.cshtml";
            ViewBag.Title = "Singular & Plural Completed";


            if (timeUp)
            {
                ViewBag.BadgeText = "Unlucky! You ran out of time.";
            }
            else if (failed)
            {
                ViewBag.BadgeText = "You used all your lives. Give it another try!";
            }
            else
            {
                if (mode == 1)
                {
                    ViewBag.BadgeText =
                    "Great job! You got all the questions right and have been awarded the grammar Badge";
                }
                else
                {
                    ViewBag.BadgeText =
                    "Great job! You got all the questions right.";
                }
            }
            ViewBag.RetryUrl =
                $"/SinPlu/Start?playerCode={playerCode}&step=1&score=0&mode={mode}&lives=3";

            ViewBag.ShowFeedback = false;

            return View("~/Views/Game/Complete.cshtml");
        }


        public IActionResult Index()
        {
            return View();
        }
    }
}
