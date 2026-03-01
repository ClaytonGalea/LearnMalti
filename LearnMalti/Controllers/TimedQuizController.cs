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

        public IActionResult Start(string playerCode, int step = 1, int score = 0, int incorrectAnswers = 0, int mode = 1, int lives = 3, int streak = 0, int correctAnswers = 0)
        {
            ViewBag.DisableGlobalTimer = true;

            // 🎲 Get 20 random items from entire DB
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
                    score,
                    correctAnswers,
                    incorrectAnswers,
                    failed = true
                });
            }

            if (step < 1 || step > items.Count)
            {
                return RedirectToAction("Completed", new
                {
                    playerCode,
                    mode,
                    score,
                    correctAnswers,
                    incorrectAnswers
                });
            }

            var current = items[step - 1];

            string questionText = current.EnglishText;
            string correctAnswer = current.DisplayMalteseWord;


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
                    questionText = $"What is the plural of \"{singular.DisplayMalteseWord}\"?";
                    correctAnswer = current.DisplayMalteseWord;
                }
            }

            var wrongChoices = _context.LearningItems
                .Where(x => x.LearningItemId != current.LearningItemId)
                .OrderBy(x => Guid.NewGuid())
                .Take(2)
                .Select(x => x.DisplayMalteseWord)
                .ToList();

            var choices = new List<string> { current.DisplayMalteseWord };
            choices.AddRange(wrongChoices);
            choices = choices.OrderBy(x => Guid.NewGuid()).ToList();

            ViewBag.QuestionText = questionText;
            ViewBag.CorrectAnswer = correctAnswer;

            ViewBag.Step = step;
            ViewBag.TotalSteps = items.Count;
            ViewBag.PlayerCode = playerCode;

            ViewBag.Score = score;
            ViewBag.CorrectAnswers = correctAnswers;
            ViewBag.IncorrectAnswers = incorrectAnswers;
            
           
            ViewBag.Mode = mode;
            ViewBag.Lives = lives;
            ViewBag.Streak = streak;

            ViewBag.Choices = choices;

            return View("Start", current);
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


        public IActionResult Completed(
         string playerCode,
         int mode,
         int score = 0,                 // correct answers
         int incorrectAnswers = 0,
         int correctAnswers = 0,
         bool failed = false)
        {
            Response.Headers["Cache-Control"] = "no-store";

            int questionsAnswered = correctAnswers + incorrectAnswers;


            var result = new TimedQuizResult
            {
                Mode = mode,
                QuestionsAnswered = questionsAnswered,
                CorrectAnswers = correctAnswers,
                IncorrectAnswers = incorrectAnswers,
                PlayerCode = playerCode,
                Score = score,
                PlayedAt = DateTime.UtcNow
            };

            _context.TimedQuizResults.Add(result);
            _context.SaveChanges();

            ViewBag.PlayerCode = playerCode;
            ViewBag.Mode = mode;
            ViewBag.Score = score;
            ViewBag.CorrectAnswers = correctAnswers;
            ViewBag.IncorrectAnswers = incorrectAnswers;
            ViewBag.QuestionsAnswered = questionsAnswered;


            ViewBag.Layout = "~/Views/Shared/_TimedQuizLayout.cshtml";
            ViewBag.Title = "Timed Quiz Completed!";

            if (mode == 1)
            {
                if (score >= 50)
                {
                    AwardBadgeIfNotExists(playerCode, 3);

                    ViewBag.BadgeText =
                        $"You answered {questionsAnswered} questions and got {correctAnswers} correct. " +
                        $"Your final score is {score}. You have been awarded the Speedrunner badge!";
                }
                else
                {
                    ViewBag.BadgeText =
                        $"You answered {questionsAnswered} questions and got {correctAnswers} correct. " +
                        $"Your final score is {score}. Reach 50 points to earn the Speedrunner badge.";
                }
            }
            else
            {
                ViewBag.BadgeText =
                    $"You answered {questionsAnswered} questions and got {correctAnswers} correct.";
            }
            ViewBag.RetryUrl =
                $"/TimedQuiz/Start?playerCode={playerCode}" +
                $"&step=1" +
                $"&score=0" +
                $"&correctAnswers=0" +
                $"&incorrectAnswers=0" +
                $"&mode={mode}" +
                $"&lives=3" +
                $"&streak=0";


            ViewBag.ShowFeedback = false;

            return View("~/Views/Game/Complete.cshtml");
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
