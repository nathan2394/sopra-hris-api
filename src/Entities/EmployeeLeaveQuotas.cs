
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "EmployeeLeaveQuotas")]
    public class EmployeeLeaveQuotas : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QuotaID { get; set; }
        public long EmployeeID { get; set; }
        public long LeaveTypeID { get; set; }
        public int? Year { get; set; }
        public int TotalQuota { get; set; }
        public int UsedQuota { get; set; }
        [NotMapped]
        public string? EmployeeName { get; set; }
    }
}
