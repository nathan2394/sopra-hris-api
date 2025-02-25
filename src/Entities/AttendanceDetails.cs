
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "AttendanceDetails")]
    public class AttendanceDetails : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AttendanceDetailID { get; set; }
        public long EmployeeID { get; set; }
        public DateTime TransDate { get; set; }
        public long? ShiftID { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime? ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }
        public string? Unattendance { get; set; }
        public long? OVTHours { get; set; }
        [NotMapped]
        public string EmployeeName { get; set; }
        [NotMapped]
        public string? DepartmentName { get; set; }
        [NotMapped]
        public string? ShiftCode { get; set; }
        [NotMapped]
        public string? ShiftName { get; set; }
    }
}
