namespace LearnMalti.Models
{
    public class PlayerProgress
    {
        public int Id { get; set; }  // Primary key

        // Links to the Player
        public int PlayerId { get; set; }
        public Player Player { get; set; }

        // Links to the learning item (word or grammar)
        public int LearningItemId { get; set; }
        public LearningItem LearningItem { get; set; }

        // Performance tracking
        public int TotalAttempts { get; set; } = 0;        // How many times the child tried this item
        public int CorrectAnswers { get; set; } = 0;       // How many times they got it correct
        public bool WasLastAnswerCorrect { get; set; }     // Was the last attempt correct?
        public DateTime? LastAttemptTime { get; set; }     // When did they last try this item?

    }
}
