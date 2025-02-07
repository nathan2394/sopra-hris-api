using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Entities
{
    public class SalaryTemplateDTO
    {
        public long EmployeeID { get; set; }
        public string Nik { get; set; }
        public string Name { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int? HKS { get; set; }
        public int? HKA { get; set; }
        public int? ATT { get; set; }
        public int? MEAL { get; set; }
        public long? ABSENT { get; set; }
        public int? Late { get; set; }
        public decimal? OVT { get; set; }
        public decimal? OtherAllowances { get; set; }
        public decimal? OtherDeductions { get; set; }
        public SalaryTemplateDTO()
        {
            Month = DateTime.Now.Month;
            Year = DateTime.Now.Year;
        }
    }
    [Keyless]
    public class SalaryPayrollSummaryDTO
    {
        public string DepartmentName { get; set; }
        public decimal AmountTransfer { get; set; }
        public int CountEmployee { get; set; }
        public decimal? AVGAmountEmployee { get; set; }
    }
    [Keyless]
    public class SalaryPayrollSummaryTotalDTO
    {
        public decimal AmountTransfer { get; set; }
        public int CountEmployee { get; set; }
        public decimal? AVGAmountEmployee { get; set; }
    }
    public class SalaryPayrollBankDTO
    {
        [Key]
        public long SalaryID { get; set; }
        public long EmployeeID { get; set; }
        public string Nik { get; set; }
        public string Name { get; set; }
        public string AccountNo { get; set; }
        public decimal? Netto { get; set; }
        public string? DepartmentCode { get; set; }
        public DateTime? TransDate { get; set; }
    }
    [Keyless]
    public class SalaryDetailReportsDTO
    {
        public long SalaryID { get; set; }
        public long Month { get; set; }
        public long Year { get; set; }
        public string Nik { get; set; }
        public string EmployeeName { get; set; }
        public string? AccountNo { get; set; }
        public string? GroupName { get; set; }
        public string? Department { get; set; }
        public string? Division { get; set; }
        public string? EmployeeType { get; set; }
        public string? EmployeeJobTitle { get; set; }
        public DateTime? StartWorkingDate { get; set; }
        public DateTime? StartJointDate { get; set; }
        public long? HKS { get; set; }
        public long? HKA { get; set; }
        public long? ATT { get; set; }
        public long? MEAL { get; set; }
        public long? ABSENT { get; set; }
        public decimal? OVT { get; set; }
        public long? Late { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal? UHMakan { get; set; }
        public decimal? UHransport { get; set; }
        public decimal? UMakan { get; set; }
        public decimal? UTransport { get; set; }
        public decimal? UJabatan { get; set; }
        public decimal? UFunctional { get; set; }
        public decimal? UTKhusus { get; set; }
        public decimal? UTOperational { get; set; }
        public decimal? ULembur { get; set; }
        public decimal? UMasaKerja { get; set; }
        public decimal? BPJS { get; set; }
        public decimal? OtherAllowances { get; set; }
        public decimal? OtherDeductions { get; set; }
        public decimal? AllowanceTotal { get; set; }
        public decimal? DeductionTotal { get; set; }
        public decimal? THP { get; set; }
        public decimal? Netto { get; set; }
    }
    [Keyless]
    public class EmployeeSalaryHistory
    {
        public long SalaryID { get; set; }
        public long Month { get; set; }
        public long Year { get; set; }
        public decimal? Netto { get; set; }
    }
    [Keyless]
    public class MasterEmployeePayroll
    {
        public long Year { get; set; }
        public long EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal? UMakan { get; set; }
        public decimal? UTransport { get; set; }
        public decimal? UJabatan { get; set; }
        public decimal? UFunctional { get; set; }
        public decimal? UTKhusus { get; set; }
        public decimal? UTOperational { get; set; }
        public decimal? UMasaKerja { get; set; }
        public decimal? ULembur { get; set; }
        public decimal? BPJS { get; set; }
        public decimal? THP { get; set; }
        public decimal? Netto { get; set; }
    }

    public class SalaryConfirmation
    {
        [Key]
        public long SalaryID { get; set; }
    }
}
