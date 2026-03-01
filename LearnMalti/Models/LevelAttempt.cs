namespace LearnMalti.Models
{
    public class LevelAttempt
    {
        public int LevelAttemptId { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; }

        public string LevelName { get; set; }

        public int Mode { get; set; }  // 0 = non-gamified, 1 = gamified

        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int DurationSeconds { get; set; }

        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }

        public decimal ScorePercentage { get; set; }

        public bool TimeRanOut { get; set; }

        public ICollection<QuestionResponse>? QuestionResponses { get; set; }
    }
}
