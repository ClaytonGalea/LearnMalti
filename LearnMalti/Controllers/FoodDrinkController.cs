using LearnMalti.Data;
using LearnMalti.Models;
using Microsoft.AspNetCore.Mvc;

namespace LearnMalti.Controllers
{
    public class FoodDrinkController : Controller
    {
        private readonly AppDbContext _context;

        public FoodDrinkController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Start(string playerCode, int step = 1, int score = 0, int mode = 1, int lives = 3)
        {

            // Get 10 Food & Drink items
            var items = _context.LearningItems
                .Where(x => x.Category == "FoodDrink")
                .OrderBy(x => x.LearningItemId)
                .Take(10)
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
                        LevelName = "FoodDrink",
                        Mode = mode,
                        StartedAt = DateTime.UtcNow,
                        TotalQuestions = items.Count,
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

            // ❌ FAILED (no lives left)
            if (lives <= 0)
            {
                return RedirectToAction("Completed", new
                {
                    playerCode,
                    score,
                    total = items.Count,
                    mode,
                    failed = true
                });
            }

            // ✅ FINISHED (step out of range)
            if (step < 1 || step > items.Count)
            {
                return RedirectToAction("Completed", new
                {
                    playerCode,
                    score,
                    total = items.Count,
                    mode
                });
            }

            if (step < 1 || step > items.Count)
                return RedirectToAction("Completed", new { playerCode, score, total = items.Count, mode });

            var current = items[step - 1];

            // Determine question type by step
            var questionType = GetQuestionType(step);

            ViewBag.QuestionType = questionType;
            ViewBag.Step = step;
            ViewBag.TotalSteps = items.Count;
            ViewBag.PlayerCode = playerCode;
            ViewBag.Score = score;
            ViewBag.Mode = mode;
            ViewBag.Lives = lives;

            // MCQ for Quiz and Sentence questions
            if (questionType == "Quiz" || questionType == "Sentence")
            {
                var wrongChoices = items
                    .Where(x => x.LearningItemId != current.LearningItemId)
                    .OrderBy(x => Guid.NewGuid())
                    .Take(2)
                    .Select(x => x.DisplayMalteseWord)
                    .ToList();

                var choices = new List<string> { current.DisplayMalteseWord };
                choices.AddRange(wrongChoices);

                ViewBag.Choices = choices.OrderBy(x => Guid.NewGuid()).ToList();
            }

            // Sentence template (only for Sentence type)
            if (questionType == "Sentence")
            {
                switch (step)
                {
                    case 8:
                        ViewBag.Sentence = "Jien niekol ____ kuljum.";
                        ViewBag.EnglishSentence = "I eat chicken every day.";
                        ViewBag.CorrectAnswer = "Tiġieġ";
                        ViewBag.Choices = new List<string> { "Tiġieġ", "Ilma", "Kafè" };
                        break;

                    case 9:
                        ViewBag.Sentence = "Nixtieq nixrob ____ jekk jogħġbok.";
                        ViewBag.EnglishSentence = "I would like to drink water, please.";
                        ViewBag.CorrectAnswer = "Ilma";
                        ViewBag.Choices = new List<string> { "Ross", "Ilma", "Laħam" };
                        break;

                    case 10:
                        ViewBag.Sentence = "Itfa’ ____ mal-kafè jekk jogħbok.";
                        ViewBag.EnglishSentence = "Add milk to the coffee, please.";
                        ViewBag.CorrectAnswer = "Ħalib";
                        ViewBag.Choices = new List<string> { "Gobon", "Ħalib", "Patata" };
                        break;
                }

                // Shuffle answers
                ViewBag.Choices = ((List<string>)ViewBag.Choices)
                    .OrderBy(x => Guid.NewGuid())
                    .ToList();
            }

            return View("Start", current);
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

        private string GetQuestionType(int step)
        {
            // 1–3 Quiz, 4–7 Fill, 8–10 Quiz
            if (step <= 3) return "Quiz";
            if (step <= 7) return "ImageInput";
            return "Sentence";
        }

        public IActionResult Completed(string playerCode, int mode, bool timeUp = false, bool failed = false)
        {
            // 🔥 UPDATE LEVEL ATTEMPT
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

            // 🔥 OPTIONAL BUT IMPORTANT (prevents reuse of old attempt)
            HttpContext.Session.Remove("CurrentAttemptId");

            ViewBag.PlayerCode = playerCode;
            ViewBag.Mode = mode;
            ViewBag.TimeUp = timeUp;
            ViewBag.Failed = failed;

            if (!timeUp && !failed)
            {
                AwardBadgeIfNotExists(playerCode, 4);
            }

            ViewBag.Layout = "~/Views/Shared/_FoodDrinkLayout.cshtml";
            //ViewBag.Title = "Food & Drink";
            ViewBag.Title = "Food & Drink Completed!";
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
                ViewBag.BadgeText =
                    "Great job! You got all the questions right and have been awarded the Food & Drink Badge";
            }

            ViewBag.RetryUrl =
                $"/FoodDrink/Start?playerCode={playerCode}&step=1&score=0&mode={mode}&lives=3";

            ViewBag.ShowFeedback = false;

            return View("~/Views/Game/Complete.cshtml");
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

        public IActionResult Index()
        {
            return View();
        }
    }
}
