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
                ViewBag.CorrectAnswer = current.Plural.MalteseText;

                var choices = new List<string>
                {
                    current.Singular.MalteseText,          // ❌ singular
                    current.Plural.MalteseText,            // ✅ plural
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
                ViewBag.CorrectAnswer = current.Plural.MalteseText;
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

        private string GetQuestionType(int step)
        {
            if (step <= 5) return "Quiz";
            if (step <= 7) return "Input";
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

        public IActionResult Completed(string playerCode, int mode, bool failed = false)
        {
            ViewBag.PlayerCode = playerCode;
            ViewBag.Mode = mode;
            ViewBag.Failed = failed;

            ViewBag.Layout = "~/Views/Shared/_SinPluLayout.cshtml";
            ViewBag.Title = "Singular & Plural Completed";

            ViewBag.BadgeText = failed
                ? "Try again!"
                : "Great job! You learned singular and plural words.";

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
