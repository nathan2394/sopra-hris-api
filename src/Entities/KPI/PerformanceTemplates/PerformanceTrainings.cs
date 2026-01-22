using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Services.API
{
    [Table(name: "PerformanceTrainings")]
    public class PerformanceTrainings : Entity
    {
        public long ID { get; set; }
        public long PerformanceConditionsID { get; set; }
        public string? Name { get; set; }
    }

    [Keyless]
    public class PerformanceTrainingsDto
    {
        public long ID { get; set; }
        public long PerformanceConditionsID { get; set; }
        public string? Name { get; set; }
    }
}