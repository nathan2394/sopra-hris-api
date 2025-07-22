
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Candidates")]
    public class Candidates : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CandidateID { get; set; }

        public string CandidateName { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string ResumeURL { get; set; }
        public string? Remarks { get; set; }
        public long JobID { get; set; }
        [NotMapped]
        public string? JobTitle { get; set; }

    }
}
