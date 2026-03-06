using LearnMalti.Data;
using LearnMalti.Models;
using Microsoft.AspNetCore.Mvc;

namespace LearnMalti.Controllers
{
    public class HangmanController : Controller
    {
        private readonly AppDbContext _context;

        public HangmanController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Start(string playerCode, int step = 1, int score = 0, int mode = 1, int lives = 6)
        {
            var word = _context.LearningItems
    .Where(x => x.MalteseText != null)
    .AsEnumerable()
    .Where(x => !x.MalteseText.Contains(" "))
    .OrderBy(x => Guid.NewGuid())
    .FirstOrDefault();

            if (word == null)
                return Content("No words found");

            HttpContext.Session.SetString("HangmanWord", word.MalteseText.ToLower());
            HttpContext.Session.SetString("GuessedLetters", "");

            ViewBag.HiddenWord = new string('_', word.MalteseText.Length);

            var player = _context.Players.FirstOrDefault(p => p.PlayerCode == playerCode);

            if (player != null)
            {
                var result = new HangmanResult
                {
                    PlayerId = player.PlayerId,
                    Word = word.MalteseText,
                    WordLength = word.MalteseText.Length,
                    TotalGuesses = 0,
                    CorrectGuesses = 0,
                    WrongGuesses = 0,
                    LivesRemaining = lives,
                    Completed = false,
                    StartedAt = DateTime.UtcNow
                };

                _context.HangmanResults.Add(result);
                _context.SaveChanges();

                HttpContext.Session.SetInt32("CurrentHangmanAttemptId", result.HangmanResultId);
            }
         
            ViewBag.PlayerCode = playerCode;
            ViewBag.Step = step;
            ViewBag.TotalSteps = 1;
            ViewBag.Score = score;
            ViewBag.Mode = mode;
            ViewBag.Lives = lives;
            ViewData["Title"] = "Hangman Mini Game";


            return View("Start");
        }

        [HttpPost]
        public IActionResult Guess(
            string playerCode,
            string letter,
            int step,
            int score,
            int mode,
            int lives)
        {
            var word = HttpContext.Session.GetString("HangmanWord");
            var guessed = HttpContext.Session.GetString("GuessedLetters") ?? "";

            letter = letter.ToLower();
            bool isCorrect = word.Contains(letter);

            if (!guessed.Contains(letter))
            {
                guessed += letter;

                if (!isCorrect)
                    lives--;

            }

            HttpContext.Session.SetString("GuessedLetters", guessed);

            string display = "";

            foreach (var c in word)
            {
                if (guessed.Contains(c))
                    display += c;
                else
                    display += "_";
            }

            // UPDATE RESULT DATA
            var attemptId = HttpContext.Session.GetInt32("CurrentHangmanAttemptId");

            if (attemptId.HasValue)
            {
                var result = _context.HangmanResults
                    .FirstOrDefault(x => x.HangmanResultId == attemptId.Value);

                if (result != null)
                {
                    result.TotalGuesses++;

                    if (isCorrect)
                        result.CorrectGuesses++;
                    else
                        result.WrongGuesses++;

                    result.LivesRemaining = lives;

                    // WIN CONDITION
                    if (display == word)
                    {
                        result.Completed = true;
                        result.CompletedAt = DateTime.UtcNow;

                        result.TimeTakenSeconds =
                            (int)(result.CompletedAt.Value - result.StartedAt).TotalSeconds;

                        ViewBag.WordCompleted = true;
                    }

                    // LOSE CONDITION
                    if (lives <= 0)
                    {
                        result.Completed = false;
                        result.CompletedAt = DateTime.UtcNow;

                        result.TimeTakenSeconds =
                            (int)(result.CompletedAt.Value - result.StartedAt).TotalSeconds;
                    }

                    _context.SaveChanges();
                }
            }


            ViewBag.HiddenWord = display;
            ViewBag.GuessedLetters = guessed;

            // REQUIRED BY _GameLevelLayout
            ViewBag.PlayerCode = playerCode;
            ViewBag.Step = step;
            ViewBag.TotalSteps = 1;
            ViewBag.Score = score;
            ViewBag.Mode = mode;
            ViewBag.Lives = lives;

            return View("Start");
        }
    }
}