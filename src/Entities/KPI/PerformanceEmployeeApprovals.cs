using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Services.API
{
    [Table(name: "PerformanceEmployeeApprovals")]
    public class PerformanceEmployeeApprovals : Entity
    {
        public long ID { get; set; }
        public long PerformanceTemplatesID { get; set; }
        public long PerformanceTemplateDetailsID { get; set; }
        public long EmployeeID { get; set; }
        public string? SubCore { get; set; }
        public string? Approvers1Category { get; set; }
        public string? Approvers1Name { get; set; }
        public long? Approvers1ID { get; set;}
        public string? Approvers2Category { get; set; }
        public string? Approvers2Name { get; set; }
        public long? Approvers2ID { get; set;}
        public string? Approvers3Category { get; set; }
        public string? Approvers3Name { get; set; }
        public long? Approvers3ID { get; set;}
        public string? Approvers4Category { get; set; }
        public string? Approvers4Name { get; set; }
        public long? Approvers4ID { get; set;}
        public string? Approvers5Category { get; set; }
        public string? Approvers5Name { get; set; }
        public long? Approvers5ID { get; set;}
    }

    [Keyless]
    public class PerformanceEmployeeApprovalsListDto
    {
        public long TemplateID { get; set; }
        public string? TemplateName { get; set; }
        public int ActiveYear { get; set; }
        public int TotalEmployees { get; set; }
        public int AssignedEmployees { get; set; }
        public int UnassignedEmployees { get; set; }
    }

    [Keyless]
    public class PerformanceEmployeeApprovalsDetailDto
    {
        public long TemplateID { get; set; }
        public string? TemplateName { get; set; }
        public long EmployeeJobTitleID { get; set; }
        public bool IsReviewed { get; set; }
        public List<PerformanceEmployeeApprovalsEmployeeDetailDto>? Employees { get; set; }
    }

    [Keyless]
    public class PerformanceEmployeeApprovalsEmployeeDetailDto
    {
        public long EmployeeID { get; set; }
        public string? EmployeeName { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public List<SubcoreApprovalDetailDto>? SubcoreDetails { get; set; }
    }

    [Keyless]
    public class SubcoreApprovalDetailDto
    {
        public long DetailID { get; set; }
        public string? SubcoreName { get; set; }
        public long? Approvers1ID { get; set; }
        public string? Approvers1Category { get; set; }
        public int? Approvers1Weight { get; set; }
        public string? Approvers1Name { get; set; }
        public long? Approvers2ID { get; set; }
        public string? Approvers2Category { get; set; }
        public int? Approvers2Weight { get; set; }
        public string? Approvers2Name { get; set; }
        public long? Approvers3ID { get; set; }
        public string? Approvers3Category { get; set; }
        public int? Approvers3Weight { get; set; }
        public string? Approvers3Name { get; set; }
        public long? Approvers4ID { get; set; }
        public string? Approvers4Category { get; set; }
        public int? Approvers4Weight { get; set; }
        public string? Approvers4Name { get; set; }
        public long? Approvers5ID { get; set; }
        public string? Approvers5Category { get; set; }
        public int? Approvers5Weight { get; set; }
        public string? Approvers5Name { get; set; }
    }

    [Keyless]
    public class AssignReviewerPayloadDto
    {
        public long TemplateID { get; set; }
        public List<EmployeeReviewerAssignmentDto>? Employees { get; set; }
    }

    [Keyless]
    public class EmployeeReviewerAssignmentDto
    {
        public long EmployeeID { get; set; }
        public List<DetailReviewerAssignmentDto>? DetailAssignments { get; set; }
    }

    [Keyless]
    public class DetailReviewerAssignmentDto
    {
        public long DetailID { get; set; }
        public long? Approvers1ID { get; set; }
        public long? Approvers2ID { get; set; }
        public long? Approvers3ID { get; set; }
        public long? Approvers4ID { get; set; }
        public long? Approvers5ID { get; set; }
    }
}