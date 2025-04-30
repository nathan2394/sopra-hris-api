
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "EmployeeIdeaDetails")]
    public class EmployeeIdeaDetails : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long EmployeeIdeaDetailID { get; set; }
        public long EmployeeIdeasID { get; set; }
        public long EmployeeID { get; set; }

    }
}
