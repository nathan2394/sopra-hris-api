
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace sopra_hris_api.Entities
{
    [Table(name: "GroupShifts")]
    public class GroupShifts : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long GroupShiftID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }
}
