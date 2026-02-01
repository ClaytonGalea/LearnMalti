namespace LearnMalti.Models
{
    public class LearningItem
    {
        public int LearningItemId { get; set; }
        public string ItemType { get; set; }
        public string MalteseText { get; set; }
        public string EnglishText { get; set; }
        public string Category { get; set; }
        public int Difficulty { get; set; } = 1;

        public string? ImageUrl { get; set; }

        public string? WordKey { get; set; }
        public string? NumberForm { get; set; }
    }
}
