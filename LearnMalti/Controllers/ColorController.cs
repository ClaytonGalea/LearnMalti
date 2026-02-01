using LearnMalti.Data;
using LearnMalti.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnMalti.Controllers
{
    public class ColorController : Controller
    {
        private readonly AppDbContext _context;

        public ColorController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Start(string playerCode, int step = 1, int score = 0, int mode = 1, int lives = 3)
        {
            var items = _context.LearningItems
                .Where(x => x.Category == "Color")
                .OrderBy(x => x.LearningItemId)
                .Take(10)
                .ToList();

            // ❌ Failed
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

            // ✅ Finished
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

            var current = items[step - 1];
            var questionType = GetQuestionType(step);

            ViewBag.QuestionType = questionType;
            ViewBag.Step = step;
            ViewBag.TotalSteps = items.Count;
            ViewBag.PlayerCode = playerCode;
            ViewBag.Score = score;
            ViewBag.Mode = mode;
            ViewBag.Lives = lives;

            // 🟦 Image → MCQ (steps 1–2)
            if (questionType == "ImageQuiz")
            {
                var wrongChoices = items
                    .Where(x => x.LearningItemId != current.LearningItemId)
                    .OrderBy(x => Guid.NewGuid())
                    .Take(2)
                    .Select(x => x.MalteseText)
                    .ToList();

                var choices = new List<string> { current.MalteseText };
                choices.AddRange(wrongChoices);

                ViewBag.Choices = choices
                    .OrderBy(x => Guid.NewGuid())
                    .ToList();
            }

            // 🟨 Color mixing (steps 3–4)
            if (questionType == "Mixing")
            {
                SetupColorMixing(step);
            }

            // 🟩 Contextual image questions (steps 5–9)
            if (questionType == "Context")
            {
                SetupContext(step);
            }

            // 🟥 Matching (step 10)
            if (questionType == "Matching")
            {
                SetupMatching(items);
            }

            return View("Start", current);
        }

        private string GetQuestionType(int step)
        {
            if (step <= 2) return "ImageQuiz";
            if (step <= 4) return "Mixing";
            if (step <= 9) return "Context";
            return "Matching";
        }

        private void SetupColorMixing(int step)
        {
            switch (step)
            {
                case 3:
                    ViewBag.Question = "Blue + Yellow = ";
                    ViewBag.CorrectAnswer = "Aħdar";
                    ViewBag.Choices = new List<string>
                    {
                        "Aħdar",
                        "Vjola",
                        "Oranġjo"
                    };
                    break;

                case 4:
                    ViewBag.Question = "Red + Yellow = ";
                    ViewBag.CorrectAnswer = "Oranġjo";
                    ViewBag.Choices = new List<string>
                    {
                        "Aħdar",
                        "Oranġjo",
                        "Blu"
                    };
                    break;
            }

            ViewBag.Choices = ((List<string>)ViewBag.Choices)
                .OrderBy(x => Guid.NewGuid())
                .ToList();
        }

        private void SetupContext(int step)
        {
            switch (step)
            {
                case 5:
                    ViewBag.ContextImage = "/images/kid_white_shirt.png";
                    ViewBag.ContextText = "What color is the boy's shirt?";
                    ViewBag.CorrectAnswer = "Abjad";
                    break;

                case 6:
                    ViewBag.ContextImage = "/images/blue_sofa.png";
                    ViewBag.ContextText = "What color is the sofa?";
                    ViewBag.CorrectAnswer = "Blu";
                    break;

                case 7:
                    ViewBag.ContextImage = "/images/red_ball.png";
                    ViewBag.ContextText = "What color is the ball";
                    ViewBag.CorrectAnswer = "Aħmar";
                    break;

                case 8:
                    ViewBag.ContextImage = "/images/green_book.png";
                    ViewBag.ContextText = "What color is the book?";
                    ViewBag.CorrectAnswer = "Aħdar";
                    break;

                case 9:
                    ViewBag.ContextImage = "/images/pink_backpack.png";
                    ViewBag.ContextText = "What color is the backpack?";
                    ViewBag.CorrectAnswer = "Roża";
                    break;
            }
        }

        private void SetupMatching(List<LearningItem> items)
        {
            var pairs = items
                .OrderBy(x => Guid.NewGuid())
                .Take(5)
                .Select(x => new
                {
                    Maltese = x.MalteseText,
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

        public IActionResult Completed(string playerCode, int mode, bool timeUp = false, bool failed = false)
        {
            ViewBag.PlayerCode = playerCode;
            ViewBag.Mode = mode;
            ViewBag.TimeUp = timeUp;
            ViewBag.Failed = failed;

            if (!timeUp && !failed)
            {
                AwardBadgeIfNotExists(playerCode, 9);
            }

            ViewBag.Layout = "~/Views/Shared/_ColorLayout.cshtml";
            ViewBag.Title = "Colors Completed!";

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
                    "Well done! You have completed the Colors level and earned the Colors Badge!";
            }

            ViewBag.RetryUrl =
                $"/Color/Start?playerCode={playerCode}&step=1&score=0&mode={mode}&lives=3";

            ViewBag.ShowFeedback = false;

            return View("~/Views/Game/Complete.cshtml");
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
        public IActionResult Index()
        {
            return View();
        }
    }
}
