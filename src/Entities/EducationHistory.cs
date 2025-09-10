
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "EducationHistory")]
    public class EducationHistory : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long EducationID { get; set; }

        public long ApplicantID { get; set; }

        public string EducationLevel { get; set; }

        public string InstitutionName { get; set; }

        public string InstitutionLocation { get; set; }

        public string Major { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }

        public decimal? GPA { get; set; }

    }
}
