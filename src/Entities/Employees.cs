
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
        public long FunctionID { get; set; }
        public long CompanyID { get; set; }
        public string? Address { get; set; }
        public decimal? BasicSalary { get; set; }
        public string? AccountNo { get; set; }
        public string? Bank { get; set; }

        [NotMapped]
        public string DepartmentName { get; set; }
        [NotMapped]
        public string GroupType { get; set; }
        [NotMapped]
        public string GroupName { get; set; }
        [NotMapped]
        public string FunctionName { get; set; }
        [NotMapped]
        public long DivisionID { get; set; }
        [NotMapped]
        public string DivisionName { get; set; }
        [NotMapped]
        public string EmployeeTypeName { get; set; }
        [NotMapped]
        public List<AllowanceDeductionEmployeeDetails> allowancedeductionDetails { get; set; }
    }
    [Keyless]
    public class AllowanceDeductionEmployeeDetails
    {
        public long AllowanceDeductionID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
    }
}
