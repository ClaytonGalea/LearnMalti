using LearnMalti.Data;
using LearnMalti.Models;
using LearnMalti.Services;
using Microsoft.AspNetCore.Mvc;

namespace LearnMalti.Controllers
{
    public class TimedQuizController : Controller
    {
        private readonly AppDbContext _context;
        private readonly GameService _gameService;

        public TimedQuizController(AppDbContext context, GameService gameService)
        {
            _context = context;
            _gameService = gameService;
        }

        public IActionResult Start(string playerCode, int step = 1, int score = 0, int incorrectAnswers = 0, int mode = 1, int lives = 3, int streak = 0, int correctAnswers = 0)
        {
            ViewBag.DisableGlobalTimer = true;
           
            var items = GetQuizItems();

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

            string questionText = GetQuestionText(current);
            string correctAnswer = GetCorrectAnswer(current);

            var choices = GetChoices(current);

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

            ViewData["Title"] = "TimedQuiz Mini Game";

            return View("Start", current);
        }

        public IActionResult Completed(string playerCode, int mode, int score = 0, int incorrectAnswers = 0, int correctAnswers = 0, bool failed = false)
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
            SetCompletionMessage(playerCode, mode, score, correctAnswers, questionsAnswered);
            ViewBag.RetryUrl = $"/TimedQuiz/Start?playerCode={playerCode}" + $"&step=1" + $"&score=0" + $"&correctAnswers=0" + $"&incorrectAnswers=0" + $"&mode={mode}" + $"&lives=3" + $"&streak=0";
            ViewBag.ShowFeedback = false;

            return View("~/Views/Game/Complete.cshtml");
        }

        private List<LearningItem> GetQuizItems()
        {
            return _context.LearningItems
                .OrderBy(x => Guid.NewGuid())
                .Take(20)
                .ToList();
        }

        private string GetQuestionText(LearningItem item)
        {
            string questionText = item.EnglishText;

            if (item.Category == "SinPlu" && item.NumberForm == "plural")
            {
                var singular = _context.LearningItems
                    .FirstOrDefault(x =>
                        x.Category == "SinPlu" &&
                        x.WordKey == item.WordKey &&
                        x.NumberForm == "singular");

                if (singular != null)
                {
                    questionText = $"What is the plural of \"{singular.DisplayMalteseWord}\"?";
                }
            }

            return questionText;
        }

        private string GetCorrectAnswer(LearningItem item)
        {
            return item.DisplayMalteseWord;
        }
        private List<string> GetChoices(LearningItem current)
        {
            var wrongChoices = _context.LearningItems
                .Where(x => x.LearningItemId != current.LearningItemId)
                .OrderBy(x => Guid.NewGuid())
                .Take(2)
                .Select(x => x.DisplayMalteseWord)
                .ToList();

            var choices = new List<string> { current.DisplayMalteseWord };
            choices.AddRange(wrongChoices);

            return choices.OrderBy(x => Guid.NewGuid()).ToList();
        }

        private void SetCompletionMessage(string playerCode, int mode, int score, int correctAnswers, int questionsAnswered)
        {
            ViewBag.ShowCompletionMessage = true;

            if (mode == 1)
            {
                if (score >= 50)
                {
                   _gameService.AwardBadgeIfNotExists(playerCode, 3);

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
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
