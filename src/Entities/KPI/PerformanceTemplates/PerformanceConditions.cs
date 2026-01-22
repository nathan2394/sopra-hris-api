using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Services.API
{
    [Table(name: "PerformanceConditions")]
    public class PerformanceConditions : Entity
    {
        public long ID { get; set; }
        public long PerformanceTemplatesID { get; set; }
        public int AgeMin { get; set; }
        public int AgeMax { get; set; }
        public string? ProfessionalBackground { get; set; }
        public string? EducationalBackground { get; set; }
        public decimal CareerYearMin { get; set; }
    }

    [Keyless]
    public class PerformanceConditionsDto
    {
        public long ID { get; set; }
        public long PerformanceTemplatesID { get; set; }
        public int AgeMin { get; set; }
        public int AgeMax { get; set; }
        public string? ProfessionalBackground { get; set; }
        public string? EducationalBackground { get; set; }
        public decimal CareerYearMin { get; set; }
        [NotMapped]
        public List<PerformanceTrainingsDto>? Training { get; set; }
    }
}