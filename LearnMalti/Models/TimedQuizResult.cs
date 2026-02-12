namespace LearnMalti.Models
{
    public class TimedQuizResult
    {
        public int TimedQuizResultId { get; set; }

        public string PlayerCode { get; set; }

        public int Score { get; set; }

        public int XP { get; set; }

        public DateTime PlayedAt { get; set; }
    }
}
