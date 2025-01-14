
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "UserLogs")]
    public class UserLogs : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserLogID { get; set; }
        public long ModuleID { get; set; }
        public long UserID { get; set; }
        public string ObjectID { get; set; }
        public string Description { get; set; }

    }
}
