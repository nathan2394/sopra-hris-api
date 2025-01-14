
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "SalaryDetails")]
    public class SalaryDetails : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SalaryDetailID { get; set; }
        public long SalaryID { get; set; }
        public long AllowanceDeductionID { get; set; }
        public decimal Amount { get; set; }
    }
}
