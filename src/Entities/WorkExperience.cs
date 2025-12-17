
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "WorkExperience")]
    public class WorkExperience : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ExperienceID { get; set; }

        public long ApplicantID { get; set; }

        public string CompanyName { get; set; }

        public string Industry { get; set; }

        public string CompanyAddress { get; set; }

        public string CompanyPhoneNumber { get; set; }

        public string JobDescription { get; set; }

        public string LastPosition { get; set; }

        public DateTime? EmploymentStartDate { get; set; }

        public DateTime? EmploymentEndDate { get; set; }

        public decimal? LastSalary { get; set; }

        public string ReasonForLeaving { get; set; }

        public bool? CanBeContacted { get; set; }
        public bool? IsCurrentJob { get; set; }

    }
}
