using LearnMalti.Data;
using LearnMalti.Models;
using LearnMalti.Services;
using Microsoft.AspNetCore.Mvc;

namespace LearnMalti.Controllers
{
    public class FoodDrinkController : Controller
    {
        //Allowing access to database
        private readonly AppDbContext _context;
        private readonly GameService _gameService;

        private const string CategoryName = "FoodDrink"; //Learning item category
        private const string LevelName = "FoodDrink";    // name used for attempts tracking
        private const int BadgeId = 4;        //Badge awarded for completion in gamified mode
        private const int TotalItems = 10;    //Total number of questions in the level
        private const int Lives = 3;          //Number of lives for the player in gamified mode

        public FoodDrinkController(AppDbContext context, GameService gameService)
        {
            _context = context;
            _gameService = gameService;
        }
        private bool HasCompletedLevel(string playerCode, string levelName)
        {
            var player = _context.Players.FirstOrDefault(p => p.PlayerCode == playerCode);

            if (player == null) return false;

            return _context.LevelAttempts.Any(a =>
                a.PlayerId == player.PlayerId &&
                a.LevelName == levelName &&
                a.CompletedAt != null);
        }
        public IActionResult Start(string playerCode, int step = 1, int mode = 1, int lives = 3)
        {
            // 🔒 LOCK: FoodDrink requires Tutorial completion
            if (!HasCompletedLevel(playerCode, "Tutorial"))
            {
                return RedirectToAction("Game", "Menu", new { playerCode, mode });
            }

            //Fetch the learning items for this level
            var items = GetFoodDrinkItems();

            //Ensure a levelAttempt record exists when the level starts
            _gameService.EnsureAttemptStarted(playerCode, LevelName, mode, items.Count, step, HttpContext);

            //If player has no lives left, redirect to completion page
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

            //If step is outside the range, redirect to completion page
            if (step < 1 || step > items.Count)
            {
                return RedirectToAction("Completed", new
                {
                    playerCode,
                    total = items.Count,
                    mode
                });
            }

            //Determine the current question based on the step
            var currentItem = items[step - 1];
            //Determine what type of question should be displayed
            var questionType = GetQuestionType(step);

            //Pass required information to the view
            ViewBag.QuestionType = questionType;
            ViewBag.Step = step;
            ViewBag.TotalSteps = items.Count;
            ViewBag.PlayerCode = playerCode;
            ViewBag.Mode = mode;
            ViewBag.Lives = lives;
            ViewBag.TimerSeconds = 45;
            ViewData["Title"] = "FoodDrink Level";

            //Generate answer choices depending on question type
            GenerateChoices(items, currentItem, questionType, step);

            return View("Start", currentItem);
        }

        public IActionResult SubmitAnswer(
           string playerCode,
           int step,
           int mode,
           int lives,
           int learningItemId,
           bool isCorrect)
        {
            //Update statistics for the current attempt
           _gameService.UpdateAttemptStats(isCorrect, HttpContext);

            //Move to the next question
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
            //Finalize the LevelAttempt record
           _gameService.FinishAttempt(timeUp, HttpContext);

            //Remove the stored attempt ID from session to prevent reuse
            HttpContext.Session.Remove("CurrentAttemptId");

            //Pass completion state to the view
            ViewBag.PlayerCode = playerCode;
            ViewBag.Mode = mode;
            ViewBag.TimeUp = timeUp;
            ViewBag.Failed = failed;

            //Award bage if level completed successfully in gamified mode
            if (!timeUp && !failed && mode == 1)
            {
               _gameService.AwardBadgeIfNotExists(playerCode, BadgeId);
            }

            //Configure completion screen UI
            ViewBag.Layout = "~/Views/Shared/_FoodDrinkLayout.cshtml";
            ViewBag.Title = "Food & Drink Completed!";
            ViewBag.BadgeText = GetCompletionText(mode, timeUp, failed);
            ViewBag.RetryUrl = $"/FoodDrink/Start?playerCode={playerCode}&step=1&score=0&mode={mode}&lives=3";
            ViewBag.ShowFeedback = false;

            //Load the completion view
            return View("~/Views/Game/Complete.cshtml");
        }




        //Retrieves the learning items associated with this level from the database
        private List<LearningItem> GetFoodDrinkItems()
        {
            return _context.LearningItems
                .Where(x => x.Category == CategoryName)
                .OrderBy(x => x.LearningItemId)
                .Take(TotalItems)
                .ToList();
        }

        //Generates answer option for the current question
        private void GenerateChoices(List<LearningItem> items, LearningItem current, string type, int step)
        {
            if (type == "Quiz" || type == "Sentence")
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


            if (type == "ImageInput")
            {
                ViewBag.CorrectAnswer = new List<string>
    {
        current.DisplayMalteseWord,
        current.DisplayMalteseWord
            .Replace("Ħ", "H")
            .Replace("ħ", "h")
    };
            }

            switch (step)
            {
                case 8:
                    SetSentence("Jien niekol ____ kuljum.",
                        "I eat chicken every day.",
                        "Tiġieġa",
                        new List<string> { "Tiġieġa", "Ilma", "Kafè" });
                    break;

                case 9:
                    SetSentence("Nixtieq nixrob ____ jekk jogħġbok.",
                        "I would like to drink water, please.",
                        "Ilma",
                        new List<string> { "Ross", "Ilma", "Laħam" });
                    break;

                case 10:
                    SetSentence("Itfa’ ____ mal-kafè jekk jogħbok.",
                        "Add milk to the coffee, please.",
                        "Ħalib",
                        new List<string> { "Ġobon", "Ħalib", "Patata" });
                    break;
            }
        }

        //Configure sentence-based questions
        private void SetSentence(string sentence, string english, string correct, List<string> choices)
        {
            ViewBag.Sentence = sentence;
            ViewBag.EnglishSentence = english;
            ViewBag.CorrectAnswer = new List<string> { correct };
            ViewBag.Choices = choices.OrderBy(x => Guid.NewGuid()).ToList();
        }
     
        //Determine question type at each step
        private string GetQuestionType(int step)
        {
            if (step <= 3) return "Quiz";
            if (step <= 7) return "ImageInput";
            return "Sentence";
        }     

        //Generates completion message dependsing on how the level ended
        private string GetCompletionText(int mode, bool timeUp, bool failed)
        {
            if (timeUp)
                return "Unlucky! You ran out of time.";

            if (failed)
                return "You used all your lives. Give it another try!";

            return mode == 1
                ? "Great job! You got all the questions right and have been awarded the Food & Drink Badge"
                : "Great job! You got all the questions right!";
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
