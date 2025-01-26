
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "TunjanganMasaKerja")]
    public class TunjanganMasaKerja : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long TunjanganMasaKerjaID { get; set; }
        public long Min { get; set; }
        public long Max { get; set; }
        public decimal Factor { get; set; }
    }
}
