namespace LearnMalti.Models
{
    public class HangmanResult
    {
        public int HangmanResultId { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; }

        public string Word { get; set; }

        public int WordLength { get; set; }

        public int TotalGuesses { get; set; }

        public int CorrectGuesses { get; set; }

        public int WrongGuesses { get; set; }

        public int LivesRemaining { get; set; }

        public bool Completed { get; set; }

        public int TimeTakenSeconds { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}
