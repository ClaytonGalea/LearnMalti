namespace LearnMalti.Models
{
    public class SurveyResponse
    {
        public int SurveyResponseId { get; set; }
        public int PlayerId { get; set; }
        public Player Player { get; set; }

        public int SurveyQuestionId { get; set; }
        public SurveyQuestion SurveyQuestion { get; set; }

        public string ResponseText { get; set; }
    }
}
