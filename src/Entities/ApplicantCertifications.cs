
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "ApplicantCertifications")]
    public class ApplicantCertifications : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CertificationID { get; set; }

        public long ApplicantID { get; set; }

        public string CertificateName { get; set; }

        public string IssuingOrganization { get; set; }

        public string CredentialID { get; set; }

        public DateTime? IssueDate { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public string? CredentialURL { get; set; }

        public string? CertificateFilePath { get; set; }

    }
}
