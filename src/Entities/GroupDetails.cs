
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "GroupDetails")]
    public class GroupDetails : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long GroupDetailID { get; set; }
        public long GroupID { get; set; }
        public long AllowanceDeductionID { get; set; }
        public decimal Amount { get; set; }
        [NotMapped]
        public string? AllowanceDeductionName { get; set; }
        [NotMapped]
        public string? AllowanceDeductionType { get; set; }
    }
}
