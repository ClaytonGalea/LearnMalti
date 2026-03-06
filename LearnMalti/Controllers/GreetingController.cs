using LearnMalti.Data;
using LearnMalti.Models;
using Microsoft.AspNetCore.Mvc;

namespace LearnMalti.Controllers
{
    public class GreetingController : Controller
    {
        private readonly AppDbContext _context;

        private const string CategoryName = "Greetings";
        private const string LevelName = "Greetings";
        private const int BadgeId = 6;
        private const int TotalItems = 10;
        private const int Lives = 3;


        public GreetingController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Start(string playerCode, int step = 1, int mode = 1, int lives = 3)
        {
            var items = GetGreetingItems();

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
           
            var attemptId = HttpContext.Session.GetInt32("CurrentAttemptId");

            HttpContext.Session.Remove("CurrentAttemptId");

            ViewBag.PlayerCode = playerCode;
            ViewBag.Mode = mode;
            ViewBag.TimeUp = timeUp;
            ViewBag.Failed = failed;

            if (!timeUp && !failed && mode == 1)
            {
                AwardBadgeIfNotExists(playerCode, BadgeId);
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
                    SetSentence("____", "What do you say when asking politely?", "Jekk joghġbok",
                        new List<string> { "Jekk joghġbok", "Tajjeb", "Narak iktar tard" });
                    break;

                case 7:
                    SetSentence("____", "Someone says: \"Bongu\". You reply saying:", "Bonġu",
                        new List<string> { "Bonġu", "Il-lejl it-tajjeb", "Grazzi" });
                    break;

                case 8:
                    SetSentence("____", "You want to ask politely.", "Jekk joghġbok",
                        new List<string> { "Jekk joghġbok", "Grazzi", "Bonġu" });
                    break;

                case 9:
                    SetSentence("____", "You are leaving. What do you say?", "Narak iktar tard",
                        new List<string> { "Narak iktar tard", "Merħba", "Kif inti?" });
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


        private void AwardBadgeIfNotExists(string playerCode, int badgeId)
        {
            //Find the player
            var player = _context.Players
                .FirstOrDefault(p => p.PlayerCode == playerCode);

            if (player == null)
                return; 

            //Check if badge already exists
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



        //Ensure LevelAttempt exists
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

        //Update attempt stats
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

        //Finalize attempt
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
