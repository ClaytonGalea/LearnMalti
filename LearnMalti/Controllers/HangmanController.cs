using LearnMalti.Data;
using LearnMalti.Models;
using LearnMalti.Services;
using Microsoft.AspNetCore.Mvc;

namespace LearnMalti.Controllers
{
    public class HangmanController : Controller
    {
        private readonly AppDbContext _context;
        private readonly GameService _gameService;

        private const int badgeId = 15;

        public HangmanController(AppDbContext context, GameService gameService)
        {
            _context = context;
            _gameService = gameService;
        }

        public IActionResult Start(string playerCode, int step = 1, int mode = 1, int lives = 6)
        {
            var word = GetRandomWord();

            if (word == null)
                return Content("No words found");

            HttpContext.Session.SetString("HangmanWord", word.DisplayMalteseWord.ToLower());
            HttpContext.Session.SetString("GuessedLetters", "");

            ViewBag.HiddenWord = new string('_', word.DisplayMalteseWord.Length);

            CreateHangmanAttempt(playerCode, word, lives);

            ViewBag.PlayerCode = playerCode;
            ViewBag.Step = step;
            ViewBag.TotalSteps = 1;
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
            int mode,
            int lives)
        {
            var word = HttpContext.Session.GetString("HangmanWord");
            var guessed = HttpContext.Session.GetString("GuessedLetters") ?? "";

            letter = letter.ToLower();
            bool isCorrect = word.Any(c => LettersMatch(c, letter[0]));

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
                bool revealed = guessed.Any(g => LettersMatch(c, g));

                if (revealed)
                    display += c;
                else
                    display += "_";
            }

            int gameStatus = UpdateHangmanResult(playerCode, display, word, isCorrect, lives);

            if (gameStatus == 1)
            {
                ViewBag.WordCompleted = true;
            }

            if (gameStatus == 2)
            {
                ViewBag.GameOver = true;
            }

            ViewBag.HiddenWord = display;
            ViewBag.GuessedLetters = guessed;

            // REQUIRED BY _GameLevelLayout
            ViewBag.PlayerCode = playerCode;
            ViewBag.Step = step;
            ViewBag.TotalSteps = 1;
            ViewBag.Mode = mode;
            ViewBag.Lives = lives;

            return View("Start");
        }

        private LearningItem GetRandomWord()
        {
            return _context.LearningItems
                .Where(x => x.MalteseText != null)
                .AsEnumerable()
                .Where(x => !x.MalteseText.Contains(" "))
                .OrderBy(x => Guid.NewGuid())
                .FirstOrDefault();
        }

        private void CreateHangmanAttempt(string playerCode, LearningItem word, int lives)
        {
            var player = _context.Players.FirstOrDefault(p => p.PlayerCode == playerCode);

            if (player == null)
                return;

            var result = new HangmanResult
            {
                PlayerId = player.PlayerId,
                Word = word.DisplayMalteseWord,
                WordLength = word.DisplayMalteseWord.Length,
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

        private int UpdateHangmanResult(string playerCode, string display, string word, bool isCorrect, int lives)
        {
            var attemptId = HttpContext.Session.GetInt32("CurrentHangmanAttemptId");

            if (!attemptId.HasValue)
                return 0;

            var result = _context.HangmanResults
                .FirstOrDefault(x => x.HangmanResultId == attemptId.Value);

            if (result == null)
                return 0;

            result.TotalGuesses++;

            if (isCorrect)
                result.CorrectGuesses++;
            else
                result.WrongGuesses++;

            result.LivesRemaining = lives;

            if (display == word)
            {
                result.Completed = true;
                result.CompletedAt = DateTime.UtcNow;

                result.TimeTakenSeconds =
                    (int)(result.CompletedAt.Value - result.StartedAt).TotalSeconds;

                _gameService.AwardBadgeIfNotExists(playerCode, badgeId);

                _context.SaveChanges();
                return 1;
            }

            if (lives <= 0)
            {
                result.Completed = false;
                result.CompletedAt = DateTime.UtcNow;

                result.TimeTakenSeconds =
                    (int)(result.CompletedAt.Value - result.StartedAt).TotalSeconds;

                _context.SaveChanges();
                return 2;
            }

            _context.SaveChanges();
            return 0;

        }

        private bool LettersMatch(char wordLetter, char guessedLetter)
        {
            wordLetter = char.ToLower(wordLetter);
            guessedLetter = char.ToLower(guessedLetter);

            if (wordLetter == guessedLetter)
                return true;

            return
                (wordLetter == 'ħ' && guessedLetter == 'h') ||
                (wordLetter == 'h' && guessedLetter == 'ħ') ||

                (wordLetter == 'ż' && guessedLetter == 'z') ||
                (wordLetter == 'z' && guessedLetter == 'ż') ||

                (wordLetter == 'ċ' && guessedLetter == 'c') ||
                (wordLetter == 'c' && guessedLetter == 'ċ') ||

                (wordLetter == 'ġ' && guessedLetter == 'g') ||
                (wordLetter == 'g' && guessedLetter == 'ġ');
        }

    }
}