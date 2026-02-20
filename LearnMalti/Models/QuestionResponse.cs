namespace LearnMalti.Models
{
    public class QuestionResponse
    {
        public int QuestionResponseId { get; set; }

        public int LevelAttemptId { get; set; }
        public LevelAttempt? LevelAttempt { get; set; }

        public int LearningItemId { get; set; }
        public LearningItem? LearningItem { get; set; }

        public string SelectedAnswer { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        public int TimeTakenSeconds { get; set; }
    }
}
