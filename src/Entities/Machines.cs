
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Machines")]
    public class Machines : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long MachineID { get; set; }
        public string Name { get; set; }
        public string SN { get; set; }
    }
}
