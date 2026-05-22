using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Services.API
{
    [Table(name: "Notifications")]
    public class Notifications : Entity
    {
        public long ID { get; set; }
        public long UserID { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool? IsRead { get; set; }
        public long? CompanyID { get; set; }
    }
}