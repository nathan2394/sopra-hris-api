using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Entities
{
    public class DashboardDTO
    {
        [Keyless]
        public class DashboardBudgetOvertimes
        {
            public long DepartmentID { get; set; }
            public string Department { get; set; }
            public decimal? TotalOvertimeHours { get; set; }
            public decimal? RemainingHours { get; set; }
        }
        [Keyless]
        public class DashboardAttendanceSummary
        {
            public long DepartmentID { get; set; }
            public string Department { get; set; }
            public int ATT { get; set; }
            public int Meals { get; set; }
            public int Late { get; set; }
            public Single OVT { get; set; }
            public int Absent { get; set; }
            public int Employee { get; set; }
        }
        [Keyless]
        public class DashboardAttendanceNormalAbnormal
        {
            public long DepartmentID { get; set; }
            public string Department { get; set; }
            public int ATT { get; set; }
            public int Abnormal { get; set; }
        }
        [Keyless]
        public class DashboardAttendanceByShift
        {
            public long DepartmentID { get; set; }
            public string Department { get; set; }
            public string ShiftCode { get; set; }
            public string ShiftName { get; set; }
            public int ATT { get; set; }
            public int Meals { get; set; }
            public int Late { get; set; }
            public Single OVT { get; set; }
            public int Absent { get; set; }
        }
        [Keyless]
        public class DashboardApproval
        {
            public long DepartmentID { get; set; }
            public string Department { get; set; }
            public string Type { get; set; }
            public int Total { get; set; }
            public int Approved { get; set; }
            public int Rejected { get; set; }
            public int Pending { get; set; }
        }
    }
}