using LearnMalti.Data;
using LearnMalti.Models;
using Microsoft.AspNetCore.Mvc;

namespace LearnMalti.Controllers
{
    public class TimedQuizController : Controller
    {
        private readonly AppDbContext _context;

        public TimedQuizController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Start(string playerCode, int step = 1, int score = 0, int mode = 1, int lives = 3)
        {
            ViewBag.DisableGlobalTimer = true;
            // 🎲 Get 3 random items from entire DB
            var items = _context.LearningItems
                .OrderBy(x => Guid.NewGuid())
                .Take(20)
                .ToList();

            if (items.Count == 0)
            {
                return Content("No items found in database.");
            }

            if (lives <= 0)
            {
                return RedirectToAction("Completed", new
                {
                    playerCode,
                    mode,
                    failed = true
                });
            }

            if (step < 1 || step > items.Count)
            {
                return RedirectToAction("Completed", new
                {
                    playerCode,
                    mode
                });
            }

            var current = items[step - 1];

            string questionText = current.EnglishText;
            string correctAnswer = current.MalteseText;

            // 🔤 Special SinPlu handling
            if (current.Category == "SinPlu" && current.NumberForm == "plural")
            {
                var singular = _context.LearningItems
                    .FirstOrDefault(x =>
                        x.Category == "SinPlu" &&
                        x.WordKey == current.WordKey &&
                        x.NumberForm == "singular");

                if (singular != null)
                {
                    questionText = $"What is the plural of \"{singular.MalteseText}\"?";
                    correctAnswer = current.MalteseText;
                }
            }

            ViewBag.QuestionText = questionText;
            ViewBag.CorrectAnswer = correctAnswer;


            var wrongChoices = _context.LearningItems
                .Where(x => x.LearningItemId != current.LearningItemId)
                .OrderBy(x => Guid.NewGuid())
                .Take(2)
                .Select(x => x.MalteseText)
                .ToList();

            var choices = new List<string> { current.MalteseText };
            choices.AddRange(wrongChoices);

            ViewBag.Choices = choices.OrderBy(x => Guid.NewGuid()).ToList();

            ViewBag.Step = step;
            ViewBag.TotalSteps = items.Count;
            ViewBag.PlayerCode = playerCode;
            ViewBag.Score = score;
            ViewBag.Mode = mode;
            ViewBag.Lives = lives;

            return View("Start", current);
        }


        public IActionResult Completed(string playerCode, int mode, int score = 0)
        {
            Response.Headers["Cache-Control"] = "no-store";

            ViewBag.PlayerCode = playerCode;
            ViewBag.Mode = mode;
            ViewBag.Score = score;

            ViewBag.Layout = "~/Views/Shared/_TimedQuizLayout.cshtml";
            ViewBag.Title = "Timed Quiz Completed!";

            ViewBag.BadgeText = $"You got {score} question{(score == 1 ? "" : "s")} right!";

            ViewBag.RetryUrl =
                $"/TimedQuiz/Start?playerCode={playerCode}&step=1&score=0&mode={mode}&lives=3";

            ViewBag.ShowFeedback = false;

            return View("~/Views/Game/Complete.cshtml");
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
