
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
        public bool? IsScreeningUser { get; set; }
        public DateTime? ScreeningUserDate { get; set; }
        public long? ScreeningUserBy { get; set; }
        public string? ScreeningUserNotes { get; set; }
        public bool? IsAssessment { get; set; }
        public long? AssessmentBy { get; set; }
        public DateTime? AssessmentDate { get; set; }
        public string? AssessmentResult { get; set; }
        public bool? IsInterview { get; set; }
        public long? InterviewBy { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string? InterviewResult { get; set; }
        public bool? IsOffer { get; set; }
        public long? OfferBy { get; set; }
        public DateTime? OfferDate { get; set; }
        public string? OfferResult { get; set; }
        public string? Status { get; set; }
        public bool? OtpVerify { get; set; }
        [NotMapped]
        public string? OTPCode { get; set; }
    }
    public class CandidateDTO
    {
        [Key]
        public long CandidateID { get; set; }
        public long ApplicantID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string MobilePhoneNumber { get; set; }

        public long? JobID { get; set; }
        public string? JobTitle { get; set; }
        public string? Location { get; set; }
        public string? Department { get; set; }
        public string? JobType { get; set; }
        public long? CompanyID { get; set; }

        public bool? IsScreening { get; set; }
        public bool? IsScreeningUser { get; set; }
        public bool? IsAssessment { get; set; }
        public bool? IsInterview { get; set; }
        public bool? IsOffer { get; set; }

        public DateTime? ScreeningDate { get; set; }
        public long? ScreeningBy { get; set; }
        public string? ScreeningNotes { get; set; }

        public DateTime? ScreeningUserDate { get; set; }
        public long? ScreeningUserBy { get; set; }
        public string? ScreeningUserNotes { get; set; }

        public long? AssessmentBy { get; set; }
        public DateTime? AssessmentDate { get; set; }
        public string? AssessmentResult { get; set; }

        public long? InterviewBy { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string? InterviewResult { get; set; }

        public long? OfferBy { get; set; }
        public DateTime? OfferDate { get; set; }
        public string? OfferResult { get; set; }

        public string? Status { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public string? Remarks { get; set; }

        public string? ResumeURL { get; set; }
        public string? Gender { get; set; }
        public string? PlaceOfBirth { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Address { get; set; }
        public DateTime? ConsentSignedAt { get; set; }
        public int? ProfileCompletion { get; set; }

        public decimal? ExpectedSalary { get; set; }
        public string? AvailabilityToStart { get; set; }
        public string? SourceOfInformation { get; set; }

        // Education
        public string? InstitutionName { get; set; }
        public string? EducationLevel { get; set; }
        public string? LastGraduationDate { get; set; }
        public string? Major { get; set; }
        public string? FirstEnrollmentDate { get; set; }
        public decimal? GPA { get; set; }

        // Work Experience
        public string? CompanyName { get; set; }
        public string? LastPosition { get; set; }
        public DateTime? EmploymentStartDate { get; set; }
        public DateTime? EmploymentEndDate { get; set; }
        public string? JobDescription { get; set; }
        public decimal? LastSalary { get; set; }
        public string? ReasonForLeaving { get; set; }

        // Certificate
        public string? CertificateName { get; set; }
        public string? IssuingOrganization { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? CredentialURL { get; set; }

        // ApplicantSkillList
        public string? ApplicantSkillList { get; set; }

        // OrganizationalHistory
        public string? OrganizationName { get; set; }
        public string? OrganizationPosition { get; set; }
        public string? LocationCity { get; set; }
        public int? PeriodYearOrganization { get; set; }
    }
}
