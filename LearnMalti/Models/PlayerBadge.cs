namespace LearnMalti.Models
{
    public class PlayerBadge
    {
        public int PlayerBadgeId { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; }

        public int BadgeId { get; set; }
        public Badge Badge { get; set; }

        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    }
}
