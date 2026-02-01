namespace LearnMalti.Models
{
    public class AssessmentResult
    {
        public int AssessmentResultId { get; set; }
        public int AssessmentId { get; set; }
        public Assessment Assessment { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; }

        public int ScoreVocabulary { get; set; }
        public int ScoreGrammar { get; set; }
        public int TotalScore { get; set; }
        public DateTime TakenAt { get; set; } = DateTime.UtcNow;
    }
}
