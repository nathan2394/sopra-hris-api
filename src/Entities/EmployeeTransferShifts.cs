
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
        public long EmployeeID { get; set; }
        public long? ShiftFromID { get; set; }
        public long? ShiftToID { get; set; }
        public DateTime? TransDate { get; set; }
        public int? HourDiff { get; set; }
        public string? Remarks { get; set; }
        public bool? IsApproved1 { get; set; }
        public bool? IsApproved2 { get; set; }
        public long? ApprovedBy1 { get; set; }
        public long? ApprovedBy2 { get; set; }
        public DateTime? ApprovedDate1 { get; set; }
        public DateTime? ApprovedDate2 { get; set; }
    }
}
