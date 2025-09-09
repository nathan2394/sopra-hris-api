
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "ApplicantSkillList")]
    public class ApplicantSkillList : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SkillListID { get; set; }

        public long ApplicantID { get; set; }

        public string SkillName { get; set; }

        public string SkillStatus { get; set; }

        public string? Notes { get; set; }
    }
}
