
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "LeadCandidates")]
    public class LeadCandidates : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string SchoolName { get; set; }
        public string LastEducation { get; set; }
        public int YearGraduated { get; set; }
        public string Major { get; set; }
        public long JobID { get; set; }
        public string LastExperience { get; set; }
        public long EventID { get; set; }
    }

    public class LeadCandidatesDto
    {
        public long ID { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string SchoolName { get; set; }
        public string LastEducation { get; set; }
        public int YearGraduated { get; set; }
        public string Major { get; set; }
        public long JobID { get; set; }
        public string LastExperience { get; set; }
        public long EventID { get; set; }
    }
}
