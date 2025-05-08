using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Entities
{
    public class EmployeeGroupShiftTemplate
    {
        public long? EmployeeID { get; set; }
        public string? Nik { get; set; }
        public string? Name { get; set; }
        public long? GroupShiftID { get; set; }
        public string? GroupShiftCode { get; set; }
        public string? GroupShiftName { get; set; }
        public long? DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
    }
    [Keyless]
    public class AttendanceSummary
    {
        public long EmployeeID { get; set; }
        public string Nik { get; set; }
        public string EmployeeName { get; set; }
        public int HKS { get; set; }
        public int HKA { get; set; }
        public int ATT { get; set; }
        public int Meals { get; set; }
        public int Late { get; set; }
        public int LateCount { get; set; }
        public int EarlyClockOut { get; set; }
        public int EarlyClockOutCount { get; set; }
        public Single OVT { get; set; }
        public int Absent { get; set; }
        public long? EmployeeTypeID { get; set; }
        public string? EmployeeTypeName { get; set; }
        public long GroupID { get; set; }
        public string? GroupType { get; set; }
        public long? DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
        public long? DivisionID { get; set; }
        public string? DivisionName { get; set; }
        public long? GroupShiftID { get; set; }
        public string? GroupShiftCode { get; set; }
        public string? GroupShiftName { get; set; }
        public string? KTP { get; set; }
        public long? FunctionID { get; set; }
        public string? FunctionName { get; set; }
    }
    [Keyless]
    public class AttendanceShift
    {
        public DateTime TransDate { get; set; }
        public long EmployeeID { get; set; }
        public bool? IsShift { get; set; }
        public long? ShiftID { get; set; }
        public string? ShiftCode { get; set; }
        public string? ShiftName { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
    [Keyless]
    public class AttendanceDTO
    {
        public long EmployeeID { get; set; }
        public long ShiftID { get; set; }
        public DateTime TransDate { get; set; }
        public List<Attendances> Attendances { get; set; }
    }
    public class ApprovalDTO
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }
        public bool? IsApproved1 { get; set; }
        public bool? IsApproved2 { get; set; }
    }
    [Keyless]
    public class AttendanceCheck
    {
        public long EmployeeID { get; set; }
        public string NIK { get; set; }
        public string EmployeeName { get; set; }
        public int IsShift { get; set; }
        public DateTime TransDate { get; set; }
        public string DayName { get; set; }
        public string ShiftCode { get; set; }
        public string ShiftName { get; set; }
        public string Unattendance { get; set; }
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
    }
    [Keyless]
    public class BulkOvertimes
    {
        public string? VoucherNo { get; set; }
        public DateTime TransDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public long? ReasonID { get; set; }
        public string Description { get; set; }
        public Single? OVTHours { get; set; }
        public List<long> EmployeeIDs { get; set; }
    }
    [Keyless]
    public class BulkEmployeeTransferShifts
    {
        public string? VoucherNo { get; set; }
        public DateTime TransDate { get; set; }
        public long? ShiftFromID { get; set; }
        public long? ShiftToID { get; set; }
        public int? HourDiff { get; set; }
        public string? Remarks { get; set; }
        public List<long> EmployeeIDs { get; set; }
    }
}
