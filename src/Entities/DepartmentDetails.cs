
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "DepartmentDetails")]
    public class DepartmentDetails : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long DepartmentDetailID { get; set; }
        public long DepartmentID { get; set; }
        public long AllowanceDeductionID { get; set; }
        public decimal Amount { get; set; }
    }
}
