
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "BudgetingOvertimes")]
    public class BudgetingOvertimes : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long BudgetingOvertimesID { get; set; }
        public int BudgetMonth { get; set; }
        public int BudgetYear { get; set; }
        public decimal TotalOvertimeHours { get; set; }
        public decimal? TotalOvertimeAmount { get; set; }
        public decimal? RemainingHours { get; set; }
        public long DepartmentID { get; set; }
        public long? DivisionID { get; set; }
        public bool? IsApproved1 { get; set; }
        public long? ApprovedBy1 { get; set; }
        public DateTime? ApprovedDate1 { get; set; }
        public string? ApprovalNotes { get; set; }
        public string? VoucherNo { get; set; }
        [NotMapped]
        public string? DepartmentName { get; set; }
        [NotMapped]
        public string? DivisionName { get; set; }
    }
}
