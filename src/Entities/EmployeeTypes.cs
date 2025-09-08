
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "EmployeeTypes")]
    public class EmployeeTypes : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long EmployeeTypeID { get; set; }
        public string Name { get; set; }
        public bool? IsOutSource { get; set; }
    }
}
