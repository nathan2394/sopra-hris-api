
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "TestResponses")]
    public class TestResponses : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ResponseID { get; set; }
        public long SessionID { get; set; }
        public long QuestionID { get; set; }
        public long SelectedAnswerID { get; set; }
    }
}
