
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "FunctionDetails")]
    public class FunctionDetails : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long FunctionDetailID { get; set; }
        public long FunctionID { get; set; }
        public long AllowanceDeductionID { get; set; }
        public decimal Amount { get; set; }
    }
}
