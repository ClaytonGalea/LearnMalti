using LearnMalti.Data;
using LearnMalti.Models;
using Microsoft.AspNetCore.Mvc;

namespace LearnMalti.Controllers
{
    public class FoodDrinkController : Controller
    {
        //Allowing access to database
        private readonly AppDbContext _context;

        private const string CategoryName = "FoodDrink"; //Learning item category
        private const string LevelName = "FoodDrink";    // name used for attempts tracking
        private const int BadgeId = 4;        //Badge awarded for completion in gamified mode
        private const int TotalItems = 10;    //Total number of questions in the level
        private const int Lives = 3;          //Number of lives for the player in gamified mode

        public FoodDrinkController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Start(string playerCode, int step = 1, int mode = 1, int lives = 3)
        {
            //Fetch the learning items for this level
            var items = GetFoodDrinkItems();

            //Ensure a levelAttempt record exists when the level starts
            EnsureAttemptStarted(playerCode, mode, items.Count, step);

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
            UpdateAttemptStats(isCorrect);

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
            FinishAttempt(timeUp);

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
                AwardBadgeIfNotExists(playerCode, BadgeId);
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

        //Ensures that a LevelAttempt record is created when the player starts the level 
        private void EnsureAttemptStarted(string playerCode, int mode, int totalQuestions, int step)
        {
            //Only create attempt on FIRST question
            if (step != 1 || HttpContext.Session.GetInt32("CurrentAttemptId") != null)
                return;

            //Finding the player in the database
            var player = _context.Players.FirstOrDefault(p => p.PlayerCode == playerCode);
            if (player == null) return;

            //Create an LevelAttempt object
            var attempt = new LevelAttempt
            {
                PlayerId = player.PlayerId,
                LevelName = LevelName,
                Mode = mode,
                StartedAt = DateTime.UtcNow,
                TotalQuestions = totalQuestions
            };

            //Save the attempt to the database
            _context.LevelAttempts.Add(attempt);
            _context.SaveChanges();

            //Store attempt ID in session for tracking
            HttpContext.Session.SetInt32("CurrentAttemptId", attempt.LevelAttemptId);
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

            if (type != "Sentence") return;

            switch (step)
            {
                case 8:
                    SetSentence("Jien niekol ____ kuljum.",
                        "I eat chicken every day.",
                        "Tiġieġ",
                        new List<string> { "Tiġieġ", "Ilma", "Kafè" });
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
            ViewBag.CorrectAnswer = correct;
            ViewBag.Choices = choices.OrderBy(x => Guid.NewGuid()).ToList();
        }

        //Updates statistics for the current attempt based on whether the player's answer was correct or not
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

       
        //Determine question type at each step
        private string GetQuestionType(int step)
        {
            if (step <= 3) return "Quiz";
            if (step <= 7) return "ImageInput";
            return "Sentence";
        }

        //Finalizes attemp statistics when level ends
        private void FinishAttempt(bool timeUp)
        {
            //Get the current attempt based on the ID stored in session from earlier
            var attemptId = HttpContext.Session.GetInt32("CurrentAttemptId");

            //If the session does not exist
            if (!attemptId.HasValue) return;

            //Retrieve the attempt from the database
            var attempt = _context.LevelAttempts
                .FirstOrDefault(a => a.LevelAttemptId == attemptId.Value);

            //If the attempt does not exist or is already completed, do nothing
            if (attempt == null || attempt.CompletedAt != null) return;

            //Mark attempt as completed and calculate final statistics
            attempt.CompletedAt = DateTime.UtcNow;
            attempt.DurationSeconds =
                (int)(attempt.CompletedAt.Value - attempt.StartedAt).TotalSeconds;

            attempt.TimeRanOut = timeUp;

            //Calculate score percentage
            attempt.ScorePercentage =
                attempt.TotalQuestions > 0
                ? Math.Round((decimal)attempt.CorrectAnswers / attempt.TotalQuestions * 100, 2)
                : 0;

            //Save the updated attempt back to the database
            _context.SaveChanges();
        }

        //Awards a badge to the player if they do not already have it
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
