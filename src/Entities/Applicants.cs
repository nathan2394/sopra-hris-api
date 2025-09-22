
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Applicants")]
    public class Applicants : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ApplicantID { get; set; }

        public string FullName { get; set; }

        public string? Gender { get; set; }

        public string? PlaceOfBirth { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? Religion { get; set; }

        public string? MaritalStatus { get; set; }

        public string? NoKTP { get; set; }

        public string? NoSIM { get; set; }

        public string? BloodType { get; set; }

        public int? HeightCM { get; set; }

        public int? WeightKG { get; set; }

        public string? Address { get; set; }

        public string? HomePhoneNumber { get; set; }

        public string MobilePhoneNumber { get; set; }

        public string Email { get; set; }

        public string? Password { get; set; }
        public DateTime? ConsentSignedAt { get; set; }
        [NotMapped]
        public long? CandidateID { get; set; }
        public int? ProfileCompletion { get; set; }
        public string? ResumeURL { get; set; }
    }

    public class ApplicantChangePassword
    {
        [Key]
        public long ApplicantID { get; set; }

        public string FullName { get; set; }
        public string MobilePhoneNumber { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }
        public long? UserUp { get; set; }
    }
}
