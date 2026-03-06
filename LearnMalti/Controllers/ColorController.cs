using LearnMalti.Data;
using LearnMalti.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnMalti.Controllers
{
    public class ColorController : Controller
    {
        private readonly AppDbContext _context;

        private const string CategoryName = "Color";
        private const string LevelName = "Color";
        private const int BadgeId = 9;
        private const int TotalItems = 10;
        private const int Lives = 3;

        public ColorController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Start(string playerCode, int step = 1, int mode = 1, int lives = 3)
        {
            var items = GetColorItems();

            EnsureAttemptStarted(playerCode, mode, items.Count, step);

            if (lives <= 0)
            {
                return RedirectToAction("Completed", new
                {
                    playerCode,
                    total = items.Count,
                    mode,
                    failed = true
                });
            }

            if (step < 1 || step > items.Count)
            {
                return RedirectToAction("Completed", new
                {
                    playerCode,
                    total = items.Count,
                    mode
                });
            }

            var current = items[step - 1];
            var questionType = GetQuestionType(step);

            ViewBag.QuestionType = questionType;
            ViewBag.Step = step;
            ViewBag.TotalSteps = items.Count;
            ViewBag.PlayerCode = playerCode;
            ViewBag.Mode = mode;
            ViewBag.Lives = lives;
            ViewBag.TimerSeconds = 30;
            ViewData["Title"] = "Color Level";

            GenerateChoices(items, current, questionType, step);

            return View("Start", current);
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

            ViewBag.Layout = "~/Views/Shared/_ColorLayout.cshtml";
            ViewBag.Title = "Colors Completed!";
            ViewBag.BadgeText = GetCompletionText(mode, timeUp, failed);
            ViewBag.RetryUrl = $"/Color/Start?playerCode={playerCode}&step=1&mode={mode}&lives=3";
            ViewBag.ShowFeedback = false;

            return View("~/Views/Game/Complete.cshtml");
        }

        private List<LearningItem> GetColorItems()
        {
            return _context.LearningItems
                .Where(x => x.Category == CategoryName)
                .OrderBy(x => x.LearningItemId)
                .Take(TotalItems)
                .ToList();
        }

        private void GenerateChoices(List<LearningItem> items, LearningItem current, string type, int step)
        {
            if (type == "Quiz")
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

            if (type == "Mixing")
            {
                SetupColorMixing(step);
            }

            if (type == "Context")
            {
                SetupContext(step);
            }

            if (type == "Matching")
            {
                SetupMatching(items);
            }
        }

        private string GetQuestionType(int step)
        {
            if (step <= 2) return "Quiz";
            if (step <= 4) return "Mixing";
            if (step <= 9) return "Context";
            return "Matching";
        }

        private void SetupColorMixing(int step)
        {
            switch (step)
            {
                case 3:
                    SetMixing("Blue + Yellow =", "Aħdar",
                        new List<string> { "Aħdar", "Vjola", "Oranġjo" });
                    break;

                case 4:
                    SetMixing("Red + Yellow =", "Oranġjo",
                        new List<string> { "Aħdar", "Oranġjo", "Blu" });
                    break;
            }
        }

        private void SetMixing(string question, string correct, List<string> choices)
        {
            ViewBag.Question = question;
            ViewBag.CorrectAnswer = correct;
            ViewBag.Choices = choices.OrderBy(x => Guid.NewGuid()).ToList();
        }

        private void SetupContext(int step)
        {
            switch (step)
            {
                case 5:
                    SetContext("/images/kid_white_shirt.png", "X'kulur hu xagħar it-tifel?", "Kannella");
                    break;

                case 6:
                    SetContext("/images/blue_sofa.png", "X'kulur hu s-sufan?", "Blu");
                    break;

                case 7:
                    SetContext("/images/red_ball.png", "X'kulur hu l-ballun?",
                        new List<string> { "Aħmar", "Ahmar" });
                    break;

                case 8:
                    SetContext("/images/green_book.png", "X'kulur hu l-ktieb?",
                        new List<string> { "Aħdar", "Ahdar" });
                    break;

                case 9:
                    SetContext("/images/pink_backpack.png", "What color is the backpack?",
                        new List<string> { "Roża", "Roza" });
                    break;
            }
        }

        private void SetContext(string image, string text, object correct)
        {
            ViewBag.ContextImage = image;
            ViewBag.ContextText = text;
            ViewBag.CorrectAnswer = correct;
        }

        private void SetupMatching(List<LearningItem> items)
        {
            var pairs = items
                .OrderBy(x => Guid.NewGuid())
                .Take(5)
                .Select(x => new
                {
                    Maltese = x.DisplayMalteseWord,
                    English = x.EnglishText
                })
                .ToList();

            ViewBag.Pairs = pairs;

            ViewBag.Maltese = pairs
                .Select(p => p.Maltese)
                .OrderBy(x => Guid.NewGuid())
                .ToList();

            ViewBag.English = pairs
                .Select(p => p.English)
                .OrderBy(x => Guid.NewGuid())
                .ToList();
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
                ? "Well done! You have completed the Colors level and earned the Colors Badge!"
                : "Well done! You have completed the Colors level!";
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
