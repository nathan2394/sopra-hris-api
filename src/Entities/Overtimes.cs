
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Overtimes")]
    public class Overtimes : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long OvertimeID { get; set; }
        public string? VoucherNo { get; set; }
        public long EmployeeID { get; set; }
        public DateTime TransDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public long? ReasonID { get; set; }
        public string Description { get; set; }
        public Single? OVTHours { get; set; }
        public bool? IsApproved1 { get; set; }
        public DateTime? ApprovedDate1 { get; set; }
        public bool? IsApproved2 { get; set; }
        public DateTime? ApprovedDate2 { get; set; }
        [NotMapped]
        public string? NIK { get; set; }
        [NotMapped]
        public string? EmployeeName { get; set; }
        [NotMapped]
        public string? DepartmentName { get; set; }
        [NotMapped]
        public long? DepartmentID { get; set; }
        [NotMapped]
        public long? GroupID { get; set; }
        [NotMapped]
        public string? GroupType { get; set; }
        [NotMapped]
        public string? GroupName { get; set; }
        [NotMapped]
        public string? ReasonCode { get; set; }
        [NotMapped]    
        public string? ReasonName { get; set; }
    }
}
