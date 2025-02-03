
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace sopra_hris_api.Entities
{
    [Table(name: "Employees")]
    public class Employees : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long EmployeeID { get; set; }
        public string Nik { get; set; }
        public string EmployeeName { get; set; }
        public string? NickName { get; set; }
        public string? PlaceOfBirth { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? KTP { get; set; }
        public DateTime StartWorkingDate { get; set; }
        public DateTime? StartJointDate { get; set; }
        public DateTime? EndWorkingDate { get; set; }
        public long EmployeeTypeID { get; set; }
        public long GroupID { get; set; }
        public long? DepartmentID { get; set; }
        public long? DivisionID { get; set; }
        public long? FunctionID { get; set; }
        public long? JobTitleID { get; set; }
        public string? Religion { get; set; }
        public string? BPJSTK { get; set; }
        public string? BPJSKES { get; set; }
        public string? Education { get; set; }
        public string? TaxStatus { get; set; }
        public string? MotherMaidenName { get; set; }
        public string? TKStatus { get; set; }
        public long CompanyID { get; set; }
        public string? AddressKTP { get; set; }
        public string? AddressDomisili { get; set; }
        public decimal? BasicSalary { get; set; }
        public string? AccountNo { get; set; }
        public string? Bank { get; set; }

        [NotMapped]
        public string? DepartmentName { get; set; }
        [NotMapped]
        public string? GroupType { get; set; }
        [NotMapped]
        public string? GroupName { get; set; }
        [NotMapped]
        public string? FunctionName { get; set; }
        [NotMapped]
        public string? DivisionName { get; set; }
        [NotMapped]
        public string? EmployeeTypeName { get; set; }
        [NotMapped]
        public string? EmployeeJobTitleName { get; set; }
        [NotMapped]
        public List<AllowanceDeductionEmployeeDetails>? AllowanceDeductionDetails { get; set; }
        [NotMapped]
        public List<MasterEmployeePayroll>? MasterEmployeePayroll { get; set; }
        [NotMapped]
        public List<EmployeeSalaryHistory> salaryHistories { get; set; }
    }
    [Keyless]
    public class AllowanceDeductionEmployeeDetails
    {
        public long ID { get; set; }
        public long AllowanceDeductionID { get; set; }
        public string AllowanceDeductionGroupType { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
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
        public decimal? ULembur { get; set; }
        public decimal? BPJS { get; set; }
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
}
