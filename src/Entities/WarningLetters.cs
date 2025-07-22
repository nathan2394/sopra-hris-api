
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "WarningLetters")]
    public class WarningLetters : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long WarningLetterID { get; set; }

        public long EmployeeID { get; set; }
        [NotMapped]
        public string? EmployeeName { get; set; }

        public DateTime WarningDate { get; set; }

        public string WarningType { get; set; }

        public string Reason { get; set; }

        public string IssuedBy { get; set; }

        public DateTime? AcknowledgementDate { get; set; }

        public string Remarks { get; set; }
    }
}
