
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "EmployeeJobTitles")]
    public class EmployeeJobTitles : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long EmployeeJobTitleID { get; set; }
        public string Name { get; set; }
    }
}
