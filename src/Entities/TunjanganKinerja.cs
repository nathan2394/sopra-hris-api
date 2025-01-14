
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "TunjanganKinerja")]
    public class TunjanganKinerja : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long TunjanganKinerjaID { get; set; }
        public long Min { get; set; }
        public long Max { get; set; }
        public decimal Factor { get; set; }
    }
}
