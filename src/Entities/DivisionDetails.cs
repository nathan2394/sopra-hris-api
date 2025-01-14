
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "DivisionDetails")]
    public class DivisionDetails : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long DivisionDetailID { get; set; }
        public long DivisionID { get; set; }
        public long AllowanceDeductionID { get; set; }
        public decimal Amount { get; set; }
    }
}
