namespace LearnMalti.Models
{
    public class SurveyQuestion
    {
        public int SurveyQuestionId { get; set; }
        public string Text { get; set; }
        public int? TargetMode { get; set; }  // null = both, 0 = control, 1 = gamified
    }
}
