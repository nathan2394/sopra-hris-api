
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "TestSessions")]
    public class TestSessions : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SessionID { get; set; }
        public long CandidateID { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
    }
    public class TestSessionDetailDTO
    {
        public long CandidateID { get; set; }
        public string CandidateName { get; set; }
        public long JobID { get; set; }
        public string JobTitle { get; set; }
        public long SessionID { get; set; }
        public string CategoryName { get; set; }
        public long QuestionID { get; set; }
        public string QuestionText { get; set; }
        public string SelectedAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public DateTime AnswerTime { get; set; }
    }

    public class TestSessionScoreByCategoryDTO
    {
        public long CandidateID { get; set; }
        public string CandidateName { get; set; }
        public long JobID { get; set; }
        public string JobTitle { get; set; }
        public string CategoryName { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public decimal PercentageScore
        {
            get
            {
                return TotalQuestions > 0 ? Math.Round((CorrectAnswers * 100m) / TotalQuestions, 2) : 0;
            }
        }
    }

    public class TestSessionOverallScoreDTO
    {
        public long CandidateID { get; set; }
        public string CandidateName { get; set; }
        public long JobID { get; set; }
        public string JobTitle { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public decimal PercentageScore
        {
            get
            {
                return TotalQuestions > 0 ? Math.Round((CorrectAnswers * 100m) / TotalQuestions, 2) : 0;
            }
        }
    }
    [Keyless]
    public class SessionQuestionRaw
    {
        public long QuestionID { get; set; }
        public string QuestionText { get; set; }
        public long AnswerID { get; set; }
        public string AnswerText { get; set; }
        public long CategoryID { get; set; }
        public string CategoryName { get; set; }
        public int Duration { get; set; }
        public long QuestionOrder { get; set; }
        public string? TestType { get; set; }
        public string? Description { get; set; }
    }
}
