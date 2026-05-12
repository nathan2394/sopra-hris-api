using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Services.API
{
    [Table(name: "PerformanceEmployeeReviewers")]
    public class PerformanceEmployeeReviewers : Entity
    {
        public long ID { get; set; }
        public long PerformanceTemplatesID { get; set; }
        public long PerformanceTemplateDetailsID { get; set; }
        public long EmployeesID { get; set; }
        public long Approvers1ID { get; set; }
        public long Approvers2ID { get; set; }
        public long Approvers3ID { get; set; }
        public long Approvers4ID { get; set; }
        public long Approvers5ID { get; set; }
        public string? Option1 { get; set; }
        public string? Option2 { get; set; }
        public string? Option3 { get; set; }
        public string? Option4 { get; set; }
        public string? Option5 { get; set; }
        public decimal TotalWeight { get; set; }
        public string? SelectedOptionDescription1 { get; set; }
        public int SelectedOptionWeight1 { get; set; }
        public decimal SelectedOptionNetWeight1 { get; set; }
        public string? SelectedOptionDescription2 { get; set; }
        public int SelectedOptionWeight2 { get; set; }
        public decimal SelectedOptionNetWeight2 { get; set; }
        public string? SelectedOptionDescription3 { get; set; }
        public int SelectedOptionWeight3 { get; set; }
        public decimal SelectedOptionNetWeight3 { get; set; }
        public string? SelectedOptionDescription4 { get; set; }
        public int SelectedOptionWeight4 { get; set; }
        public decimal SelectedOptionNetWeight4 { get; set; }
        public string? SelectedOptionDescription5 { get; set; }
        public int SelectedOptionWeight5 { get; set; }
        public decimal SelectedOptionNetWeight5 { get; set; }
        public string? Remarks1 { get; set; }
        public string? Remarks2 { get; set; }
        public string? Remarks3 { get; set; }
        public string? Remarks4 { get; set; }
        public string? Remarks5 { get; set; }
    }

    [Keyless]
    public class ReviewerFormsDto
    {
        public long EmployeesID { get; set; }
        public string? EmployeeName { get; set; }
        public List<FormDetailsDto>? FormDetails { get; set; }
    }

    [Keyless]
    public class FormDetailsDto
    {
        public long ID { get; set; }
        public string? Question { get; set; }
        public string? Option1 { get; set; }
        public string? Option2 { get; set; }
        public string? Option3 { get; set; }
        public string? Option4 { get; set; }
        public string? Option5 { get; set; }
        public string? Remarks { get; set; }
        public int SelectedOption { get; set; }
        public int ApproverNo { get; set; }
    }

    [Keyless]
    public class ToBeReviewedEmployeesDto
    {
        public long ID { get; set; }
        public string? Name { get; set; }
        public string? JobTitle { get; set; }
    }

    [Keyless]
    public class OptionWeightRow
    {
        public int Option { get; set; }
        public int Weight { get; set; }
    }

    [Keyless]
    public class EmployeeScoresDto
    {
        public long EmployeesID { get; set; }
        public string? EmployeeName { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public decimal? PP { get; set; }
        public decimal? PK { get; set; }
        public decimal? PM { get; set; }
        public int? CompletedQuestion { get; set; }
        public int? TotalQuestion { get; set; }
        public decimal? TotalScore { get; set; }

        [NotMapped]
        public EmployeeScoreDetailCategoriesDto? ScoreDetails { get; set; }
    }

    [Keyless]
    public class EmployeeScoreDetailCategoriesDto
    {
        public List<EmployeeScoreDetailsDto>? PP { get; set; }
        public List<EmployeeScoreDetailsDto>? PK { get; set; }
        public List<EmployeeScoreDetailsDto>? PM { get; set; }
    }

    [Keyless]
    public class EmployeeScoreDetailsDto
    {
        public long ID { get; set; }
        public string? Question { get; set; }
        public string? Type { get; set; }
        public decimal Weight { get; set; }
        public decimal Score { get; set; }
        public int TotalApprover { get; set; }
        public int Approved { get; set; }
        public List<string>? Remarks { get; set; }
        public string? Remarks1 { get; set; }
        public string? Remarks2 { get; set; }
        public string? Remarks3 { get; set; }
        public string? Remarks4 { get; set; }
        public string? Remarks5 { get; set; }
    }
}