
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace sopra_hris_api.Entities
{
    [Keyless]
    [Table(name: "AttendanceDetails")]
    public class AttendanceDetails 
    {
        public long EmployeeID { get; set; }
        public string Nik { get; set; }
        public string EmployeeName { get; set; }
        public DateTime TransDate { get; set; }
        public string DayName { get; set; }
        public string? ShiftCode { get; set; }
        public string? ShiftName { get; set; }
        public int? WorkingDays { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime? StartBufferTime { get; set; }
        public DateTime? EndBufferTime { get; set; }
        public string? Unattendance { get; set; }
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        public int? Late { get; set; }
        public int? Ovt { get; set; }
        public int? EarlyClockOut { get; set; }
        public int? Meals { get; set; }
        public Single? EffectiveHours { get; set; }
    }
}
