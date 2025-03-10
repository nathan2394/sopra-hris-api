using Microsoft.EntityFrameworkCore;

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
        public int Late { get; set; }
        public int OVT { get; set; }
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
    }
}
