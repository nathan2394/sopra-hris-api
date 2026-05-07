
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Groups")]
    public class Groups : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long GroupID { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int? TransportAllowance { get; set; }
        public int? MealAllowance { get; set; }
        public int? PositionalAllowance { get; set; }
        public int? OperationalAllowance { get; set; }
        public int? ProbationMonth { get; set; }
        public string? OtherBenefit { get; set; }
    }
}
