using LearnMalti.Data;
using LearnMalti.Models;
using Microsoft.AspNetCore.Mvc;

namespace LearnMalti.Controllers
{
    public class TutorialController : Controller
    {
        //Allowing access to database
        private readonly AppDbContext _context;

        //Constant valies for the controller
        private const string CategoryName = "Tutorial"; //Used to fetch tutorial learning items from the database
        private const int TutorialBadgeId = 1; //Used to award the badge
        private const int TutorialItemCount = 5; //Total number of questions

        public TutorialController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Start(string playerCode, int step = 1, int mode = 1)
        {
            //Retrieves the tutorial learning items from the database
            var items = GetTutorialItems();

            //If step is outside the range, redirect to completion page
            if (step < 1 || step > items.Count)
                return RedirectToAction("Completed", new { playerCode, mode });

            //Determine the current question based on the step
            var currentItem = items[step - 1];

            //Generate the choices for the current question (1 correct + 2 wrong)
            var choices = GenerateChoices(items, currentItem);

            //Data passed to the view using ViewBag
            ViewBag.Choices = choices; //List of answer options
            ViewBag.Step = step;       //Current question number
            ViewBag.TotalSteps = items.Count;  //Total number of questions
            ViewBag.PlayerCode = playerCode;   //Player ID
            ViewBag.Mode = mode;              // Game mode (Gamified || non Gamified)
            ViewBag.TimerSeconds = 60;        //Time limit for answering the question
            ViewData["Title"] = "Tutorial Level"; //Page title

            //Return the current learning item as the view model
            return View(currentItem);
        }

        public IActionResult Completed(string playerCode, int mode, bool timeUp = false)
        {
            //Pass player and mode information to the view using ViewBag
            ViewBag.PlayerCode = playerCode;
            ViewBag.Mode = mode;
            ViewBag.TimeUp = timeUp;

            //Award the tutorial badge if the player completed the tutorial successfully without running out of time
            if (!timeUp)
            {
                AwardBadgeIfNotExists(playerCode, TutorialBadgeId);
            }

            //Configure completion screen UI
            ViewBag.Layout = "~/Views/Shared/_GameLevelLayout.cshtml";
            ViewBag.Title = "Tutorial Completed!";
            ViewBag.BadgeText = "Great job! You got all the questions right and have been awarded the Tutorial Master Badge";
            ViewBag.RetryUrl = $"/Tutorial/Start?playerCode={playerCode}&step=1&score=0&mode={mode}";
            ViewBag.ShowFeedback = true;

            //Load the completion view
            return View("~/Views/Game/Complete.cshtml");
        }

        //Retrieves tutorial learning items from the database
        private List<LearningItem> GetTutorialItems()
        {
            return _context.LearningItems
                .Where(x => x.Category == CategoryName) //Filter only items in the "Tutorial" category
                .OrderBy(x => x.LearningItemId)         //Maintain consistent order
                .Take(TutorialItemCount)                //Limit the number of questions
                .ToList();
        }

        //Generates multiple choice answers for the current question, including the correct answer and two random wrong answers
        private List<String> GenerateChoices(List<LearningItem> items, LearningItem currentItem)
        {
            //Select 2 incorrect answers from the pool of items randomly and ensure they are not the same as the current correct answer
            var wrongChoices = items
                .Where(x => x.LearningItemId != currentItem.LearningItemId)
                .OrderBy(x => Guid.NewGuid())
                .Take(2)
                .Select(x => x.DisplayMalteseWord)
                .ToList();

            //Add the correct answer
            var choices = new List<string> { currentItem.DisplayMalteseWord };
            choices.AddRange(wrongChoices);

            //Shuffle the choices order
            return choices.OrderBy(x => Guid.NewGuid()).ToList();
        }

        //Awards a badge to the player if they do not already have it
        private void AwardBadgeIfNotExists(string playerCode, int badgeId)
        {
            //Find the player based on the unique player code
            var player = _context.Players
                .FirstOrDefault(p => p.PlayerCode == playerCode);

            //Safety check in case player not found
            if (player == null)
                return;

            //Check if the player already has the badge to prevent duplicates
            bool alreadyHasBadge = _context.PlayerBadges
                .Any(pb => pb.PlayerId == player.PlayerId && pb.BadgeId == badgeId);

            //If the player does not already have the badge, create a new PlayerBadge entry to award it
            if (!alreadyHasBadge)
            {
                var playerBadge = new PlayerBadge
                {
                    PlayerId = player.PlayerId, // FK
                    BadgeId = badgeId,
                    EarnedAt = DateTime.Now
                };

                //Save the badge to the database
                _context.PlayerBadges.Add(playerBadge);
                _context.SaveChanges();
            }
        }

    }
}
