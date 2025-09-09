
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
        public string? PortfolioLink { get; set; }
        public string? Remarks { get; set; }
        public long JobID { get; set; }
        [NotMapped]
        public string? JobTitle { get; set; }
        [NotMapped]
        public string? JobType { get; set; }
        [NotMapped]
        public string? Department { get; set; }
        [NotMapped]
        public string? Location { get; set; }
        [NotMapped]
        public long? CompanyID { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public long? ApplicantID { get; set; }
        public bool? IsScreening { get; set; }
        public DateTime? ScreeningDate { get; set; }
        public long? ScreeningBy { get; set; }
        public string? ScreeningNotes { get; set; }
        public bool? IsAssessment { get; set; }
        public DateTime? AssessmentDate { get; set; }
        public string? AssessmentResult { get; set; }
        public bool? IsInterview { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string? InterviewResult { get; set; }
        public string? Status { get; set; }
        public bool? OtpVerify { get; set; }
        [NotMapped]
        public string? OTPCode { get; set; }
    }
}
