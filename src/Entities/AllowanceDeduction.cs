
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "AllowanceDeduction")]
    public class AllowanceDeduction : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AllowanceDeductionID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string AmountType { get; set; }

    }
}
