using LearnMalti.Data;
using LearnMalti.Models;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace LearnMalti.Controllers
{
    public class TravelController : Controller
    {
        //Allowing access to database
        private readonly AppDbContext _context;

        private const string CategoryName = "Travel"; //Learning item category
        private const string LevelName = "Travel";    //Name used for attempts tracking
        private const int BadgeId = 8;                //Badge awarded for completion in gamified mode
        private const int TotalItems = 10;            //Total number of questions in the level
        private const int DefaultLives = 3;           //Number of lives for the player in gamified mode

        public TravelController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Start(string playerCode, int step = 1, int mode = 1, int lives = 3)
        {
            //Rettrieve all travel learning items
            var items = GetTravelItems();

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
            ViewData["Title"] = "Travel Level";

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

        public IActionResult Completed(string playerCode, int total, int mode, bool timeUp = false, bool failed = false)
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
            ViewBag.Layout = "~/Views/Shared/_TravelLayout.cshtml";
            ViewBag.Title = "Travel Completed!";
            ViewBag.BadgeText = GetCompletionText(mode, timeUp, failed);
            ViewBag.RetryUrl =$"/Travel/Start?playerCode={playerCode}&step=1&mode={mode}&lives=3";
            ViewBag.ShowFeedback = false;
            return View("~/Views/Game/Complete.cshtml");
        }

        public IActionResult Index()
        {
            return View();
        }

        //Retrieves the learning items associated with this level from the database
        private List<LearningItem> GetTravelItems()
        {
            return _context.LearningItems
                .Where(x => x.Category == CategoryName)
                .OrderBy(x => x.LearningItemId)
                .Take(TotalItems)
                .ToList();
        }
        //Determine question type at each step
        private string GetQuestionType(int step)
        {
            // 1–4 Quiz, 5–7 Sentence, 8–10 Image Input
            if (step <= 4) return "Quiz";
            if (step <= 7) return "Sentence";
            return "ImageInput";
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
                    .Select(x => x.MalteseText)
                    .ToList();

                var choices = new List<string> { current.MalteseText };
                choices.AddRange(wrongChoices);

                ViewBag.Choices = choices.OrderBy(x => Guid.NewGuid()).ToList();
            }

            if (type != "Sentence") return;

            switch (step)
            {
                case 5:
                    SetSentence("Trid immorru dawra bil- ____?",
                        "Would you like to go for a drive with the car?",
                        "Karozza",
                        new List<string> { "Mappa", "Karozza", "Muntanja" });
                    break;

                case 6:
                    SetSentence("Naħseb li tlifna t-triq, iftaħ il- ____.",
                        "I think we are lost, open the map.",
                        "Mappa",
                        new List<string> { "Ajruplan", "Mappa", "Triq" });
                    break;

                case 7:
                    SetSentence("Din hija t-____ it-tajba.",
                        "This is the right road.",
                        "Triq",
                        new List<string> { "Triq", "Ħalib", "Patata" });
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


        //Awards a badge to the player if they do not already have it
        private void AwardBadgeIfNotExists(string playerCode, int badgeId)
        {
            //Find the player
            var player = _context.Players
                .FirstOrDefault(p => p.PlayerCode == playerCode);

            if (player == null)
                return; // safety check

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

        //Generates completion message dependsing on how the level ended
        private string GetCompletionText(int mode, bool timeUp, bool failed)
        {
            if (timeUp)
                return "Unlucky! You ran out of time.";

            if (failed)
                return "You used all your lives. Give it another try!";

            return mode == 1
                ? "Great job! You got all the questions right and have been awarded the Travel Badge"
                : "Great job! You got all the questions right!";
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
       
    }
}
