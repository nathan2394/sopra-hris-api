
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "UnattendanceTypes")]
    public class UnattendanceTypes : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UnattendanceTypeID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int? Quota { get; set; }
    }
}
