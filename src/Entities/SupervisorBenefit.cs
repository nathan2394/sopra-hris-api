
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "SupervisorBenefit")]
    public class SupervisorBenefit : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SupervisorBenefitID { get; set; }

        public long EmployeeID { get; set; }

        public int RewardMonth { get; set; }

        public int RewardYear { get; set; }

        public decimal TotalOvertimeHours { get; set; }
        [NotMapped]
        public string? EmployeeName { get; set; }
    }
}
