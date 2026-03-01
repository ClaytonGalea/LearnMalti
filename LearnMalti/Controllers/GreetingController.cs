using LearnMalti.Data;
using LearnMalti.Models;
using Microsoft.AspNetCore.Mvc;

namespace LearnMalti.Controllers
{
    public class GreetingController : Controller
    {
        private readonly AppDbContext _context;

        public GreetingController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Start(string playerCode, int step = 1, int score = 0, int mode = 1, int lives = 3)
        {
            var items = _context.LearningItems
                .Where(x => x.Category == "Greetings" && x.Difficulty == 1)
                .OrderBy(x => x.LearningItemId)
                .Take(10)
                .ToList();

            // 🔥 CREATE LEVEL ATTEMPT (only first step and no active attempt)
            if (step == 1 && HttpContext.Session.GetInt32("CurrentAttemptId") == null)
            {
                var player = _context.Players
                    .FirstOrDefault(p => p.PlayerCode == playerCode);

                if (player != null)
                {
                    var attempt = new LevelAttempt
                    {
                        PlayerId = player.PlayerId,
                        LevelName = "Greetings",
                        Mode = mode,
                        StartedAt = DateTime.UtcNow,
                        TotalQuestions = items.Count,
                        CorrectAnswers = 0,
                        IncorrectAnswers = 0,
                        RetryCount = 0,
                        TimeRanOut = false
                    };

                    _context.LevelAttempts.Add(attempt);
                    _context.SaveChanges();

                    HttpContext.Session.SetInt32("CurrentAttemptId", attempt.LevelAttemptId);
                }
            }

            // ❌ Failed
            if (lives <= 0)
            {
                return RedirectToAction("Completed", new
                {
                    playerCode,
                    score,
                    total = items.Count,
                    mode,
                    failed = true
                });
            }

            // ✅ Finished
            if (step < 1 || step > items.Count)
            {
                return RedirectToAction("Completed", new
                {
                    playerCode,
                    score,
                    total = items.Count,
                    mode
                });
            }

            var current = items[step - 1];
            var questionType = GetQuestionType(step);

            ViewBag.QuestionType = questionType;
            ViewBag.Step = step;
            ViewBag.TotalSteps = items.Count;
            ViewBag.PlayerCode = playerCode;
            ViewBag.Score = score;
            ViewBag.Mode = mode;
            ViewBag.Lives = lives;

            // 🟦 Quiz & Sentence (MCQ-style)
            if (questionType == "Quiz" || questionType == "Sentence")
            {
                var wrongChoices = items
                    .Where(x => x.LearningItemId != current.LearningItemId)
                    .OrderBy(x => Guid.NewGuid())
                    .Take(2)
                    .Select(x => x.MalteseText)
                    .ToList();

                var choices = new List<string> { current.MalteseText };
                choices.AddRange(wrongChoices);

                ViewBag.Choices = choices
                    .OrderBy(x => Guid.NewGuid())
                    .ToList();
            }
            if (questionType == "Sentence")
            {
                SetupSentence(step);
            }
            // 🟨 Dialogue
            if (questionType == "Dialogue")
            {
                SetupDialogue(step);
            }

            // 🟥 Matching (final question)
            if (questionType == "Matching")
            {
                SetupMatching(items);
            }

            return View("Start", current);
        }

        private string GetQuestionType(int step)
        {
            if (step <= 3) return "Quiz";
            if (step <= 6) return "Sentence";
            if (step <= 9) return "Dialogue";
            return "Matching"; // step 10
        }
        private void SetupSentence(int step)
        {
            switch (step)
            {
                case 4:
                    ViewBag.EnglishSentence = "How do you greet someone in the morning?";
                    ViewBag.Sentence = "____";
                    ViewBag.CorrectAnswer = "Bongu";
                    ViewBag.Choices = new List<string>
            {
                "Bongu",
                "Il-lejl it-tajjeb",
                "Grazzi"
            };
                    break;

                case 5:
                    ViewBag.EnglishSentence = "How do you say thank you politely?";
                    ViewBag.Sentence = "____";
                    ViewBag.CorrectAnswer = "Grazzi";
                    ViewBag.Choices = new List<string>
            {
                "Grazzi",
                "Merħba",
                "Skużani"
            };
                    break;

                case 6:
                    ViewBag.EnglishSentence = "What do you say when asking politely?";
                    ViewBag.Sentence = "____";
                    ViewBag.CorrectAnswer = "Jekk jogħġbok";
                    ViewBag.Choices = new List<string>
            {
                "Jekk jogħġbok",
                "Tajjeb",
                "Narak iktar tard"
            };
                    break;
            }

            ViewBag.Choices = ((List<string>)ViewBag.Choices)
                .OrderBy(x => Guid.NewGuid())
                .ToList();
        }
        public IActionResult SubmitAnswer(
    string playerCode,
    int step,
    int score,
    int mode,
    int lives,
    int learningItemId,
    bool isCorrect)
        {
            var attemptId = HttpContext.Session.GetInt32("CurrentAttemptId");

            if (attemptId.HasValue)
            {
                var attempt = _context.LevelAttempts
                    .FirstOrDefault(a => a.LevelAttemptId == attemptId.Value);

                if (attempt != null)
                {
                    if (isCorrect)
                        attempt.CorrectAnswers++;
                    else
                        attempt.IncorrectAnswers++;

                    _context.SaveChanges();
                }
            }

            return RedirectToAction("Start", new
            {
                playerCode,
                step = isCorrect ? step + 1 : step,
                score,
                mode,
                lives
            });
        }

        private void SetupDialogue(int step)
        {
            switch (step)
            {
                case 7:
                    ViewBag.Prompt = "Someone says: \"Bongu\". You reply saying:";
                    ViewBag.CorrectAnswer = "Bongu";
                    ViewBag.Choices = new List<string>
                    {
                        "Bongu",
                        "Il-lejl it-tajjeb",
                        "Grazzi"
                    };
                    break;

                case 8:
                    ViewBag.Prompt = "You want to ask politely.";
                    ViewBag.CorrectAnswer = "Jekk jogħgbok";
                    ViewBag.Choices = new List<string>
                    {
                        "Jekk jogħgbok",
                        "Grazzi",
                        "Bongu"
                    };
                    break;

                case 9:
                    ViewBag.Prompt = "You are leaving. What do you say?";
                    ViewBag.CorrectAnswer = "Narak iktar tard";
                    ViewBag.Choices = new List<string>
                    {
                        "Narak iktar tard",
                        "Merħba",
                        "Kif inti?"
                    };
                    break;
            }

            ViewBag.Choices = ((List<string>)ViewBag.Choices)
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

        public IActionResult Completed(string playerCode, int mode, bool timeUp = false, bool failed = false)
        {
            // 🔥 FINALIZE LEVEL ATTEMPT
            var attemptId = HttpContext.Session.GetInt32("CurrentAttemptId");

            if (attemptId.HasValue)
            {
                var attempt = _context.LevelAttempts
                    .FirstOrDefault(a => a.LevelAttemptId == attemptId.Value);

                if (attempt != null && attempt.CompletedAt == null)
                {
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
            }

            HttpContext.Session.Remove("CurrentAttemptId");
            ViewBag.PlayerCode = playerCode;
            ViewBag.Mode = mode;
            ViewBag.TimeUp = timeUp;
            ViewBag.Failed = failed;

            if (!timeUp && !failed && mode == 1)
            {
                AwardBadgeIfNotExists(playerCode, 6);
            }

            ViewBag.Layout = "~/Views/Shared/_GreetingLayout.cshtml";
            ViewBag.Title = "Greetings Completed!";

            if (timeUp)
            {
                ViewBag.BadgeText = "Unlucky! You ran out of time.";
            }
            else if (failed)
            {
                ViewBag.BadgeText = "You used all your lives. Give it another try!";
            }
            else
            {
                if (mode == 1)
                {
                    ViewBag.BadgeText =
                    "Great job! You got all the questions right and have been awarded the Greetings Badge";
                }
                else
                {
                    ViewBag.BadgeText =
                    "Great job! You got all the questions right!";
                }
            }

            ViewBag.RetryUrl =
                $"/Greeting/Start?playerCode={playerCode}&step=1&score=0&mode={mode}&lives=3";

            ViewBag.ShowFeedback = false;

            return View("~/Views/Game/Complete.cshtml");
        }
        public IActionResult Index()
        {
            return View();
        }

    }


}
