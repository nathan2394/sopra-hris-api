
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Salary")]
    public class Salary : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long SalaryID { get; set; }
        public long EmployeeID { get; set; }
        public long Month { get; set; }
        public long Year { get; set; }
        public long? HKS { get; set; }
        public long? HKA { get; set; }
        public long? ATT { get; set; }
        public long? MEAL { get; set; }
        public long? ABSENT { get; set; }
        public decimal? OVT { get; set; }
        public long? Late { get; set; }
        public decimal? BasicSalary { get; set; }
        public decimal? AllowanceTotal { get; set; }
        public decimal? DeductionTotal { get; set; }
        public decimal? Netto { get; set; }
        public string? PayrollType { get; set; }
        [NotMapped]
        public string Nik { get; set; }
        [NotMapped]
        public string Name { get; set; }
        [NotMapped]
        public string AccountNo { get; set; }
        [NotMapped]
        public string Bank { get; set; }
        [NotMapped]
        public long EmployeeTypeID { get; set; }
        [NotMapped]
        public string EmployeeTypeName { get; set; }
        [NotMapped]
        public long GroupID { get; set; }
        [NotMapped]
        public string GroupName { get; set; }
        [NotMapped]
        public long FunctionID { get; set; }
        [NotMapped]
        public string FunctionName { get; set; }
        [NotMapped]
        public long DivisionID { get; set; }
        [NotMapped]
        public string DivisionName { get; set; }
        [NotMapped]
        public DateTime? TransDate { get; set; }
        public Salary()
        {
            Month = DateTime.Now.Month;
            Year = DateTime.Now.Year;
        }
    }
}
