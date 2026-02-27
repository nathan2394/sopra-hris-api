using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Services.API
{
    [Table(name: "PerformanceCompetencies")]
    public class PerformanceCompetencies : Entity
    {
        public long ID { get; set; }
        public long PerformanceTemplatesID { get; set; }
        public string? Name { get; set; }
    }

    [Keyless]
    public class PerformanceCompetenciesDto
    {
        public long ID { get; set; }
        public long PerformanceTemplatesID { get; set; }
        public string? Name { get; set; }
        public List<PerformanceCompetencyDetailsDto>? CompetencyDetails { get; set; }
    }
}