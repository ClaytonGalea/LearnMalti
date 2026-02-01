using LearnMalti.Data;
using LearnMalti.Models;
using Microsoft.AspNetCore.Mvc;

namespace LearnMalti.Controllers
{
    public class TutorialController : Controller
    {
        private readonly AppDbContext _context;

        public TutorialController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Start(string playerCode, int step = 1, int score = 0, int mode = 1)
        {
            var items = _context.LearningItems
                .Where(x => x.Category == "Tutorial")
                .OrderBy(x => x.LearningItemId)
                .Take(5)
                .ToList();

            if (step < 1 || step > items.Count)
                return RedirectToAction("Completed", new { playerCode, score, mode });

            var current = items[step - 1];

            // Generate 3 choices (1 correct + 2 random wrong)
            var wrongChoices = items
                .Where(x => x.LearningItemId != current.LearningItemId)
                .OrderBy(x => Guid.NewGuid())
                .Take(2)
                .Select(x => x.MalteseText)
                .ToList();

            var choices = new List<string> { current.MalteseText };
            choices.AddRange(wrongChoices);

            // Shuffle choices randomly
            choices = choices.OrderBy(x => Guid.NewGuid()).ToList();

            ViewBag.Choices = choices;
            ViewBag.Step = step;
            ViewBag.TotalSteps = items.Count;
            ViewBag.PlayerCode = playerCode;
            ViewBag.Score = score;
            ViewBag.Mode = mode;

            return View(current);
        }


        /*public IActionResult Completed(string playerCode, int score, int total, int mode, bool timeUp = false)
        {
            ViewBag.PlayerCode = playerCode;
            ViewBag.Score = score;
            ViewBag.TotalSteps = total;
            ViewBag.Mode = mode;
            ViewBag.TimeUp = timeUp;

            if (!timeUp)
            {
                AwardBadgeIfNotExists(playerCode, 1);
            }

            return View();
        }*/

        public IActionResult Completed(string playerCode, int mode, bool timeUp = false)
        {
            ViewBag.PlayerCode = playerCode;
            ViewBag.Mode = mode;
            ViewBag.TimeUp = timeUp;

            if (!timeUp)
            {
                AwardBadgeIfNotExists(playerCode, 1);
            }

            ViewBag.Layout = "~/Views/Shared/_GameLevelLayout.cshtml";
            ViewBag.Title = "Tutorial Completed!";
            ViewBag.BadgeText = "Great job! You got all the questions right and have been awarded the Tutorial Master Badge";
            ViewBag.RetryUrl = $"/Tutorial/Start?playerCode={playerCode}&step=1&score=0&mode={mode}";
            ViewBag.ShowFeedback = true;

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


    }
}
