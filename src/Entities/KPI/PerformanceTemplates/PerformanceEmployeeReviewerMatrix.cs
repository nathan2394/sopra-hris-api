using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Services.API
{
    [Table(name: "PerformanceEmployeeReviewerMatrix")]
    public class PerformanceEmployeeReviewerMatrix
    {
        [Key]
        public long EmployeeJobTitleID { get; set; }
        public long? Atasan1ID { get; set; }
        public string? Atasan1Name { get; set; }
        public long? Atasan2ID { get; set; }
        public string? Atasan2Name { get; set; }
        public long? TeamQAID { get; set; }
        public string? TeamQAName { get; set; }
    }
}