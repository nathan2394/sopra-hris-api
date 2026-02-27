using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Services.API
{
    [Table(name: "PerformanceCompetencyDetails")]
    public class PerformanceCompetencyDetails : Entity
    {
        public long ID { get; set; }
        public long PerformanceCompetenciesID { get; set; }
        public string? Description { get; set; }
    }

    [Keyless]
    public class PerformanceCompetencyDetailsDto
    {
        public long ID { get; set; }
        public long PerformanceCompetenciesID { get; set; }
        public string? Description { get; set; }
    }
}