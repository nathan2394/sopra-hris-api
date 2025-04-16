
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
        public long DepartmentID { get; set; }
        [NotMapped]
        public string? DepartmentName { get; set; }
    }
}
