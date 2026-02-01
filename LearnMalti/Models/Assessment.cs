namespace LearnMalti.Models
{
    public class Assessment
    {
        public int AssessmentId { get; set; }
        public string Name { get; set; }
        public int AssessmentType { get; set; }  // 0 Pre, 1 Post
    }
}
