using LearnMalti.Data;
using LearnMalti.Models;
using LearnMalti.Services;
using Microsoft.AspNetCore.Mvc;

namespace LearnMalti.Controllers
{
    public class GreetingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly GameService _gameService;

        private const string CategoryName = "Greetings";
        private const string LevelName = "Greetings";
        private const int BadgeId = 6;
        private const int TotalItems = 10;
        private const int Lives = 3;


        public GreetingController(AppDbContext context, GameService gameService)
        {
            _context = context;
            _gameService = gameService; 
        }
        public IActionResult Start(string playerCode, int step = 1, int mode = 1, int lives = 3)
        {
            var items = GetGreetingItems();

           _gameService.EnsureAttemptStarted(playerCode, LevelName, mode, items.Count, step, HttpContext);

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

            var currentItem = items[step - 1];
            var questionType = GetQuestionType(step);

            ViewBag.QuestionType = questionType;
            ViewBag.Step = step;
            ViewBag.TotalSteps = items.Count;
            ViewBag.PlayerCode = playerCode;
            ViewBag.Mode = mode;
            ViewBag.Lives = lives;
            ViewBag.TimerSeconds = 45;
            ViewData["Title"] = "Greetings Level";

            GenerateChoices(items, currentItem, questionType, step);

            return View("Start", currentItem);
        }

       
        public IActionResult SubmitAnswer(string playerCode, int step, int mode, int lives, int learningItemId, bool isCorrect)
        {
          _gameService.UpdateAttemptStats(isCorrect, HttpContext);

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
            _gameService.FinishAttempt(timeUp, HttpContext);
           
            var attemptId = HttpContext.Session.GetInt32("CurrentAttemptId");

            HttpContext.Session.Remove("CurrentAttemptId");

            ViewBag.PlayerCode = playerCode;
            ViewBag.Mode = mode;
            ViewBag.TimeUp = timeUp;
            ViewBag.Failed = failed;

            if (!timeUp && !failed && mode == 1)
            {
                _gameService.AwardBadgeIfNotExists(playerCode, BadgeId);
            }

            ViewBag.Layout = "~/Views/Shared/_GreetingLayout.cshtml";
            ViewBag.Title = "Greetings Completed!";
            ViewBag.BadgeText = GetCompletionText(mode, timeUp, failed);
            ViewBag.RetryUrl = $"/Greeting/Start?playerCode={playerCode}&step=1&mode={mode}&lives=3";
            ViewBag.ShowFeedback = false;

            return View("~/Views/Game/Complete.cshtml");
        }


        private List<LearningItem> GetGreetingItems()
        {
            return _context.LearningItems
                .Where(x => x.Category == CategoryName && x.Difficulty == 1)
                .OrderBy(x => x.LearningItemId)
                .Take(TotalItems)
                .ToList();
        }

        //Generate answers depending on question type
        private void GenerateChoices(List<LearningItem> items, LearningItem current, string type, int step)
        {
            if (type == "Quiz" || type == "Sentence")
            {
                var wrongChoices = items
                    .Where(x => x.LearningItemId != current.LearningItemId)
                    .OrderBy(x => Guid.NewGuid())
                    .Take(2)
                    .ToList();

                var choices = new List<LearningItem> { current };
                choices.AddRange(wrongChoices);

                ViewBag.Choices = choices
                    .OrderBy(x => Guid.NewGuid())
                    .ToList();
            }

            if (type == "Sentence")
            {
                SetupSentence(step);
            }

            if (type == "Matching")
            {
                SetupMatching(items);
            }
        }

        //Sentence questions
        private void SetupSentence(int step)
        {
            ViewBag.Sentence = "____";

            switch (step)
            {
                case 4:
                    SetSentence("____", "How do you greet someone in the morning?", "Bonġu",
                        new List<string> { "Bonġu", "Il-lejl it-tajjeb", "Grazzi" });
                    break;

                case 5:
                    SetSentence("____", "How do you say thank you politely?", "Grazzi",
                        new List<string> { "Grazzi", "Merħba", "Skużani" });
                    break;

                case 6:
                    SetSentence("____", "What do you say when asking politely?", "Jekk jogħġbok",
                        new List<string> { "Jekk jogħġbok", "Tajjeb", "Narak aktar tard" });
                    break;

                case 7:
                    SetSentence("____", "Someone says: \"Bongu\". You reply saying:", "Bonġu",
                        new List<string> { "Bonġu", "Il-lejl it-tajjeb", "Grazzi" });
                    break;

                case 8:
                    SetSentence("____", "You want to ask politely.", "Jekk jogħġbok",
                        new List<string> { "Jekk jogħġbok", "Grazzi", "Bonġu" });
                    break;

                case 9:
                    SetSentence("____", "You are leaving. What do you say?", "Narak aktar tard",
                        new List<string> { "Narak aktar tard", "Merħba", "Kif inti?" });
                    break;
            }
        }

        private void SetSentence(string sentence, string english, string correct, List<string> words)
        {
            ViewBag.Sentence = sentence;
            ViewBag.EnglishSentence = english;
            ViewBag.CorrectAnswer = correct;

            var choices = _context.LearningItems
                .Where(x => words.Contains(x.MalteseText))
                .ToList();

            ViewBag.Choices = choices
                .OrderBy(x => Guid.NewGuid())
                .ToList();
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

        private string GetQuestionType(int step)
        {
            if (step <= 3) return "Quiz";
            if (step <= 9) return "Sentence";
            return "Matching";
        }

        private string GetCompletionText(int mode, bool timeUp, bool failed)
        {
            if (timeUp)
                return "Unlucky! You ran out of time.";

            if (failed)
                return "You used all your lives. Give it another try!";

            return mode == 1
                ? "Great job! You got all the questions right and have been awarded the Greetings Badge"
                : "Great job! You got all the questions right!";
        }

        public IActionResult Index()
        {
            return View();
        }

    }
}
