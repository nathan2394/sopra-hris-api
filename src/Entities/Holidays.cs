
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Holidays")]
    public class Holidays : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long HolidayID { get; set; }
        public DateTime TransDate {  get; set; }
        public string Description { get; set; }

    }
}
