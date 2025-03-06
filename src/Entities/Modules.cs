
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Modules")]
    public class Modules : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ModuleID { get; set; }
        public string Group { get; set; }
        public string Name { get; set; }
        public string Route { get; set; }
        public long ParentID { get; set; }
        public bool IsChild { get; set; }
    }
}
