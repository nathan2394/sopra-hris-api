
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "EmployeeShifts")]
    public class EmployeeShifts : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long EmployeeShiftID { get; set; }
        public long EmployeeID { get; set; }
        public long? ShiftID { get; set; }
        public DateTime? TransDate { get; set; }
        public long? GroupShiftID { get; set; }
        [NotMapped]
        public string EmployeeName { get; set; }
        [NotMapped]
        public string ShiftCode { get; set; }
        [NotMapped]
        public string ShiftName { get; set; }
        [NotMapped]
        public string GroupShiftCode { get; set; }
        [NotMapped]
        public string GroupShiftName { get; set; }
    }
}
