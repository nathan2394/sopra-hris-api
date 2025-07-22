
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "AttendanceIncentive")]
    public class AttendanceIncentive : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AttendanceIncentiveID { get; set; }

        public string SalaryType { get; set; }

        public string Code { get; set; }

        public decimal? Incentive { get; set; }

        public string Description { get; set; }
    }
}
