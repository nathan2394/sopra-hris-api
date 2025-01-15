
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "SalaryHistory")]
    public class SalaryHistory : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public long SalaryHistoryID { get; set; }
        public long EmployeeID { get; set; }
        public string NIK { get; set; }
        public long Month { get; set; }
        public long Year { get; set; }
        public long? HKS { get; set; }
        public long? HKA { get; set; }
        public long? ATT { get; set; }
        public long? MEAL { get; set; }
        public long? ABSENT { get; set; }
        public decimal? OVT { get; set; }
        public long? Late { get; set; }
        public decimal? OtherAllowances { get; set; }
        public decimal? OtherDeductions { get; set; }
    }
}
