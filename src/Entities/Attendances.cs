
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Attendances")]
    public class Attendances : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AttendanceID { get; set; }
        public long EmployeeID { get; set; }
        public DateTime ClockIn { get; set; }
        public string? Description { get; set; }
    }
}
