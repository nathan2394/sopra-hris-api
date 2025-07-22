
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace sopra_hris_api.Entities
{
    [Table(name: "EmployeeIdeas")]
    public class EmployeeIdeas : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long EmployeeIdeasID { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string? Implementation { get; set; }

        public string? Impact { get; set; }

        public string? EstimatedImplementationTime { get; set; }

        public string? AttachmentLink { get; set; }

        public DateTime? ReviewDate { get; set; }

        public string? ReviewerComments { get; set; }

        public DateTime? TrialDate { get; set; }

        public DateTime? MonitoringEndDate { get; set; }

        public string? ActualImplementationDetails { get; set; }

        public DateTime? ImplementationDate { get; set; }

        public string? ActualImpactDetails { get; set; }

        public long? SubmittedByUserID { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public string SubmissionType { get; set; }

        public string Status { get; set; }
        [NotMapped]
        public string? EmployeeName { get; set; }
        [NotMapped]
        public long? DepartmentID { get; set; }
        [NotMapped]
        public string? DepartmentName { get; set; }
    }
}
