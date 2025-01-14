
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "EmployeeDetails")]
    public class EmployeeDetails : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long EmployeeDetailID { get; set; }
        public long EmployeeID { get; set; }
        public long AllowanceDeductionID { get; set; }
        public decimal Amount { get; set; }
    }
}
