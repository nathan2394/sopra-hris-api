
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
        public long? OVT { get; set; }
        public long? Late { get; set; }
        public decimal? BasicSalary { get; set; }
        public decimal? AllowanceTotal { get; set; }
        public decimal? DeductionTotal { get; set; }
        public decimal? Netto { get; set; }
        public string? PayrollType { get; set; }
        public Salary()
        {
            Month = DateTime.Now.Month;
            Year = DateTime.Now.Year;
        }
    }
}
