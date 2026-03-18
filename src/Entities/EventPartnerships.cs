
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "EventPartnerships")]
    public class EventPartnerships : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }
        public string SchoolName { get; set; }
        public string PicName { get; set; }
        public string PicPhoneNumber { get; set; }
        public string PicEmail { get; set; }
        public string Address { get; set; }
    }

    public class EventPartnershipsDto
    {
        public long ID { get; set; }
        public string SchoolName { get; set; }
        public string PicName { get; set; }
        public string PicPhoneNumber { get; set; }
        public string PicEmail { get; set; }
        public string Address { get; set; }
    }
}
