using System;

namespace LearnMalti.Models
{
    public class TimedQuizResult
    {
        public int TimedQuizResultId { get; set; }
        public int Mode { get; set; }

        public int QuestionsAnswered { get; set; }

        public int CorrectAnswers { get; set; }

        public int IncorrectAnswers { get; set; }

        public string PlayerCode { get; set; }

        public int Score { get; set; }
        public DateTime PlayedAt { get; set; }

    }
}
