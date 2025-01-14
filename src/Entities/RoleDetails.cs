
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "RoleDetails")]
    public class RoleDetails : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long RoleDetailID { get; set; }
        public long RoleID { get; set; }
        public long ModuleID { get; set; }
        public long IsCreate { get; set; }
        public long IsRead { get; set; }
        public long IsUpdate { get; set; }
        public long IsDelete { get; set; }

    }
}
