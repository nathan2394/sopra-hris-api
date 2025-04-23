
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Configurations")]
    public class Configurations
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }
        public string Code { get; set; }
        public string Value { get; set; }
    }
}
