namespace LearnMalti.Models
{
    public class Player
    {
        public int PlayerId { get; set; }
        public string PlayerCode { get; set; } = string.Empty;
        public int Mode { get; set; }  // 0 = Control, 1 = Gamified
        public int YearGroup { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int CurrentLevel { get; set; } = 1;
        public int CurrentXp { get; set; } = 0;
    }
}
