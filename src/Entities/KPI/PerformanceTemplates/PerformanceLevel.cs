using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Services.API
{
    [Table(name: "PerformanceLevel")]
    public class PerformanceLevel : Entity
    {
        public long ID { get; set; }
        public int Level { get; set; }
        public int PP { get; set; }
        public int PK { get; set; }
        public int PM { get; set; }
    }
}