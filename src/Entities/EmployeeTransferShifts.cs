
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace sopra_hris_api.Entities
{
    [Table(name: "EmployeeTransferShifts")]
    public class EmployeeTransferShifts : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long EmployeeTransferShiftID { get; set; }
        public string? VoucherNo { get; set; }
        public long EmployeeID { get; set; }
        public long? ShiftFromID { get; set; }
        public long? ShiftToID { get; set; }
        public DateTime TransDate { get; set; }
        public int? HourDiff { get; set; }
        public string? Remarks { get; set; }
        public bool? IsApproved1 { get; set; }
        public bool? IsApproved2 { get; set; }
        public long? ApprovedBy1 { get; set; }
        public long? ApprovedBy2 { get; set; }
        public DateTime? ApprovedDate1 { get; set; }
        public DateTime? ApprovedDate2 { get; set; }
        public string? ApprovalNotes { get; set; }
        [NotMapped]
        public string? NIK { get; set; }
        [NotMapped]
        public string? EmployeeName { get; set; }
        [NotMapped]
        public string? DepartmentName { get; set; }
        [NotMapped]
        public long? DepartmentID { get; set; }
        [NotMapped]
        public string? DivisionName { get; set; }
        [NotMapped]
        public long? DivisionID { get; set; }
        [NotMapped]
        public long? GroupID { get; set; }
        [NotMapped]
        public string? GroupType { get; set; }
        [NotMapped]
        public string? GroupName { get; set; }
    }
}
