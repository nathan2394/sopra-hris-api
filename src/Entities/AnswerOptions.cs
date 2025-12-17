
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "AnswerOptions")]
    public class AnswerOptions : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AnswerID { get; set; }
        public long QuestionID { get; set; }
        public string AnswerText { get; set; }
        public bool IsCorrect { get; set; }
        public decimal ScoreValue { get; set; }

    }
}
