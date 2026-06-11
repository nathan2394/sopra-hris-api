namespace sopra_hris_api.Entities
{
    public class CreateEmployeeFromPortalRequest
    {
        public long CandidateJobOfferID { get; set; }
        public long CandidateID { get; set; }
        public long JobID { get; set; }
        public long? ApplicantID { get; set; }
        public string? EmployeeName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? KTP { get; set; }
        public string? NPWP { get; set; }
        public string? PlaceOfBirth { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public DateTime? StartWorkingDate { get; set; }
        public string? EmployeeTypeName { get; set; }
        public long CompanyID { get; set; }
        public long GroupID { get; set; }
        public string? Religion { get; set; }
        public string? Education { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Address { get; set; }
        public decimal? BasicSalary { get; set; }
        public string? Bank { get; set; }
        public string? AccountNo { get; set; }
    }

    public class CreateEmployeeFromPortalResponse
    {
        public long CandidateJobOfferID { get; set; }
        public long CandidateID { get; set; }
        public long? EmployeeID { get; set; }
        public long? UserID { get; set; }
        public string? Nik { get; set; }
        public string? KTP { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
