using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Services.API
{
    [Table(name: "PerformanceApproverCategories")]
    public class PerformanceApproverCategories : Entity
    {
        public long ID { get; set; }
        public string? Name { get; set; }
    }

    [Keyless]
    public class PerformanceApproverCategoriesDto
    {
        public long ID { get; set; }
        public string? Name { get; set; }
    }
}