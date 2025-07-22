
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "EmployeeMonthlyReward")]
    public class EmployeeMonthlyReward : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long MonthlyRewardID { get; set; }

        public long? EmployeeID { get; set; }

        public int? RewardMonth { get; set; }

        public int? RewardYear { get; set; }

        public string RewardCode { get; set; }
        [NotMapped]
        public string? EmployeeName { get; set; }
    }
}
