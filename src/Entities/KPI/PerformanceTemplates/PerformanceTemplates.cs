using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Services.API
{
    [Table(name: "PerformanceTemplates")]
    public class PerformanceTemplates : Entity
    {
        public long ID { get; set; }
        public long DepartmentsID { get; set; }
        public long DivisionsID { get; set; }
        public long EmployeeJobTitlesID { get; set; }
        public string? MainValue { get; set; }
        public string? GeneralGoal { get; set;}
        public Boolean Status { get; set; }
        public int ActiveYear { get; set; }
    }

    [Keyless]
    public class PerformanceTemplatesDto
    {
        public long ID { get; set; }
        public long EmployeeJobTitlesID { get; set; }
        public long DepartmentsID { get; set; }
        public long DivisionsID { get; set; }
        public string? MainValue { get; set; }
        public string? GeneralGoal { get; set;}
        public int ActiveYear { get; set; }
        [NotMapped]
        public PerformanceConditionsDto? Condition { get; set; }
        [NotMapped]
        public PerformanceTemplateDetailSectionDto? TemplateDetails { get; set; }
        [NotMapped]
        public List<PerformanceCompetenciesDto>? Competency { get; set; }
    }

    [Keyless]
    public class PerformanceTemplateListDto
    {
        public long ID { get; set; }
        public string? Name { get; set; }
        public string? Department { get; set; }
        public int Periode { get; set; }
        public DateTime? TransDate { get; set; }
        public Boolean Status { get; set; }
    }
}