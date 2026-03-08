using LearnMalti.Data;
using LearnMalti.Models;
using LearnMalti.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnMalti.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayerApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PlayerApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePlayer()
        {
            string code;

            do
            {
                code = PlayerCodeGenerator.Generate();
            }
            while (await _context.Players.AnyAsync(p => p.PlayerCode == code));

            var player = new Player
            {
                PlayerCode = code,
                Mode = null
            };

            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            return Ok(player);
        }

        [HttpPost("setmode")]
        public async Task<IActionResult> SetMode(string playerCode, int mode)
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.PlayerCode == playerCode);

            if (player == null)
                return NotFound("Player not found");

            player.Mode = mode;

            await _context.SaveChangesAsync();

            return Ok(player);
        }

        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetTopPlayers()
        {
            var topPlayers = await _context.Players
                .OrderByDescending(p => p.CurrentLevel)
                .ThenByDescending(p => p.CurrentXp)
                .Take(5)
                .Select(p => new {
                    p.PlayerCode,
                    p.CurrentLevel,
                    p.CurrentXp
                })
                .ToListAsync();

            return Ok(topPlayers);
        }


        [HttpPost("addxp")]
        public async Task<IActionResult> AddXp(string playerCode, int amount)
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.PlayerCode == playerCode);

            if (player == null)
                return NotFound("Player not found");

            player.CurrentXp += amount;

            // Optional: handle level-up
            if (player.CurrentXp >= 100)
            {
                player.CurrentLevel ++;
                player.CurrentXp -= 100;
            }

            await _context.SaveChangesAsync();

            return Ok(player);
        }
    }
}
