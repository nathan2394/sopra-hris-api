
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Reasons")]
    public class Reasons : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ReasonID { get; set; }
        public string Code {  get; set; }
        public string Name { get; set; }
    }
}
