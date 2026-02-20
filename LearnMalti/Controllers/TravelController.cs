using LearnMalti.Data;
using LearnMalti.Models;
using Microsoft.AspNetCore.Mvc;

namespace LearnMalti.Controllers
{
    public class TravelController : Controller
    {
        private readonly AppDbContext _context;

        public TravelController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Start(string playerCode, int step = 1, int score = 0, int mode = 1, int lives = 3)
        {
            // Get 10 Food & Drink items
            var items = _context.LearningItems
                .Where(x => x.Category == "Travel")
                .OrderBy(x => x.LearningItemId)
                .Take(10)
                .ToList();
            // 🔥 CREATE LEVEL ATTEMPT (only first question)
            if (step == 1 && HttpContext.Session.GetInt32("CurrentAttemptId") == null)
            {
                var player = _context.Players
                    .FirstOrDefault(p => p.PlayerCode == playerCode);

                if (player != null)
                {
                    var attempt = new LevelAttempt
                    {
                        PlayerId = player.PlayerId,
                        LevelName = "Travel",
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
                    .Select(x => x.MalteseText)
                    .ToList();

                var choices = new List<string> { current.MalteseText };
                choices.AddRange(wrongChoices);

                ViewBag.Choices = choices.OrderBy(x => Guid.NewGuid()).ToList();
            }

            // Sentence template (only for Sentence type)
            if (questionType == "Sentence")
            {
                switch (step)
                {
                    case 5:
                        ViewBag.Sentence = "Trid immorru dawra bil- ____?";
                        ViewBag.EnglishSentence = "Would you like to go for a drive with the car?";
                        ViewBag.CorrectAnswer = "Karozza";
                        ViewBag.Choices = new List<string> { "Mappa", "Karozza", "Muntanja" };
                        break;

                    case 6:
                        ViewBag.Sentence = "Naħseb li tlifna t-triq, iftaħ il- ____.";
                        ViewBag.EnglishSentence = "I think we are lost, open the map.";
                        ViewBag.CorrectAnswer = "Mappa";
                        ViewBag.Choices = new List<string> { "Ajruplan", "Mappa", "Triq" };
                        break;

                    case 7:
                        ViewBag.Sentence = "Din hija t-____ it-tajba.";
                        ViewBag.EnglishSentence = "This is the right road.";
                        ViewBag.CorrectAnswer = "Triq";
                        ViewBag.Choices = new List<string> { "Triq", "Ħalib", "Patata" };
                        break;
                }

                // Shuffle answers
                ViewBag.Choices = ((List<string>)ViewBag.Choices)
                    .OrderBy(x => Guid.NewGuid())
                    .ToList();
            }

            return View("Start", current);
        }


        private string GetQuestionType(int step)
        {
            // 1–4 Quiz, 5–7 Sentence, 8–10 Image Input
            if (step <= 4) return "Quiz";
            if (step <= 7) return "Sentence";
            return "ImageInput";
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

            if (attemptId.HasValue)
            {
                var attempt = _context.LevelAttempts
                    .FirstOrDefault(a => a.LevelAttemptId == attemptId.Value);

                if (attempt != null)
                {
                    if (isCorrect)
                        attempt.CorrectAnswers++;
                    else
                        attempt.IncorrectAnswers++;

                    _context.SaveChanges();
                }
            }

            return RedirectToAction("Start", new
            {
                playerCode,
                step = isCorrect ? step + 1 : step,
                score,
                mode,
                lives
            });
        }

        public IActionResult Completed(string playerCode, int score, int total, int mode, bool timeUp = false, bool failed = false)
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

            if (!timeUp && !failed)
            {
                AwardBadgeIfNotExists(playerCode, 8);
            }
            ViewBag.Layout = "~/Views/Shared/_TravelLayout.cshtml";
            ViewBag.Title = "Travel Completed!";
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
                    "Great job! You got all the questions right and have been awarded the Travel Badge";
            }

            ViewBag.RetryUrl =
                $"/Travel/Start?playerCode={playerCode}&step=1&score=0&mode={mode}&lives=3";

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
