using LearnMalti.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnMalti.Controllers
{
    public class MenuController : Controller
    {
        private readonly AppDbContext _context;

        public MenuController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult SelectMode()
        {
            return View();
        }

        public async Task<IActionResult> Game(string playerCode, int mode)
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.PlayerCode == playerCode);

            if (player == null)
            {
                return NotFound("Player not found");
            }

            var badges = await _context.PlayerBadges
            .Where(pb => pb.PlayerId == player.PlayerId)
            .Select(pb => pb.Badge)
            .ToListAsync();

            ViewBag.Player = player;
            ViewBag.Mode = mode;
            ViewBag.Badges = badges;

            return View();
        }

        public IActionResult Feedback(string playerCode)
        {
            if (string.IsNullOrWhiteSpace(playerCode))
                return BadRequest("Missing player code");

            string googleFormUrl =
                "https://docs.google.com/forms/d/e/1FAIpQLSfXoHacvYmlsn_SFmq8V_WdMgsZLSl2CkguDLj_spa_EJx7hA/viewform" +
                "?entry.933124037=" + Uri.EscapeDataString(playerCode);

            return Redirect(googleFormUrl);
        }


    }
}
