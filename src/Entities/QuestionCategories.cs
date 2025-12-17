using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "QuestionCategories")]
    public class QuestionCategories : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public decimal? Weight { get; set; }
        public int? TotalQuestions { get; set; }
        public int? Duration { get; set; }
        public string? TestType { get; set; }
    }
}
