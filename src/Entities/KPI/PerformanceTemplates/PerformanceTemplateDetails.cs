using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Services.API
{
    [Table(name: "PerformanceTemplateDetails")]
    public class PerformanceTemplateDetails : Entity
    {
        public long ID { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public long PerformanceTemplatesID { get; set; }
        public long PerformanceTemplateDetailGroupsID { get; set; }
        public int Weight { get; set; }
        public string? MediaDescription { get; set; }
        public string? Option1 { get; set; }
        public string? Option2 { get; set; }
        public string? Option3 { get; set; }
        public string? Option4 { get; set; }
        public string? Option5 { get; set; }
        public string? Approver1 { get; set; }
        public string? Approver2 { get; set; }
        public string? Approver3 { get; set; }
    }

    [Keyless]
    public class PerformanceTemplateDetailsDto
    {
        public long ID { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public long PerformanceTemplatesID { get; set; }
        public long PerformanceTemplateDetailGroupsID { get; set; }
        public int Weight { get; set; }
        public string? MediaDescription { get; set; }
        public string? Option1 { get; set; }
        public string? Option2 { get; set; }
        public string? Option3 { get; set; }
        public string? Option4 { get; set; }
        public string? Option5 { get; set; }
        public string? Approver1 { get; set; }
        public string? Approver2 { get; set; }
        public string? Approver3 { get; set; }
    }

    [Keyless]
    public class PerformanceTemplateDetailSectionDto
    {
        public List<PerformanceTemplateDetailsDto>? PP { get; set; }
        public List<PerformanceTemplateDetailsDto>? PK { get; set; }
        public List<PerformanceTemplateDetailsDto>? PM { get; set; }
    }
}