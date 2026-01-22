using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Services.API
{
    [Table(name: "PerformanceTemplateDetailGroups")]
    public class PerformanceTemplateDetailGroups : Entity
    {
        public long ID { get; set; }
        public string? Name { get; set; } 
        public string? Type { get; set; }
    }
}