
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Questions")]
    public class Questions : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QuestionID { get; set; }
        public long CategoryID { get; set; }
        public string QuestionText { get; set; }
    }
}
