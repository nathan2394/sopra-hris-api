
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Functions")]
    public class Functions : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long FunctionID { get; set; }
        public long DivisionID { get; set; }
        public string Name { get; set; }
    }
}
