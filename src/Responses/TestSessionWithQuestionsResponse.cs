namespace sopra_hris_api.Responses
{
    public class TestSessionWithQuestionsResponse
    {
        public long SessionID { get; set; }
        public long CandidateID { get; set; }
        public DateTime StartTime { get; set; }
        public List<QuestionSectionDTO> Sections { get; set; }

        public TestSessionWithQuestionsResponse()
        {
            Sections = new List<QuestionSectionDTO>();
        }
    }

    public class QuestionSectionDTO
    {
        public long CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public int? TotalQuestions { get; set; }
        public int? Duration { get; set; }
        public string? TestType { get; set; }
        public List<QuestionWithAnswersDTO> Questions { get; set; }

        public QuestionSectionDTO()
        {
            Questions = new List<QuestionWithAnswersDTO>();
        }
    }

    public class QuestionWithAnswersDTO
    {
        public long QuestionID { get; set; }
        public string QuestionText { get; set; }
        public List<AnswerOptionDTO> Answers { get; set; }

        public QuestionWithAnswersDTO()
        {
            Answers = new List<AnswerOptionDTO>();
        }
    }

    public class AnswerOptionDTO
    {
        public long AnswerID { get; set; }
        public string AnswerText { get; set; }
    }
}
