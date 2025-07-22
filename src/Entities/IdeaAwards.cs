
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace sopra_hris_api.Entities
{
    [Table(name: "IdeaAwards")]
    public class IdeaAwards : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long IdeaAwardID { get; set; }

        public long EmployeeIdeasID { get; set; }

        public decimal? AwardAmount { get; set; }

        public string? Implementation { get; set; }

        public DateTime? AwardDate { get; set; }
        public int? RewardMonth { get; set; }

        public int? RewardYear { get; set; }

        public string? AwardCategory { get; set; }

        public string? Notes { get; set; }
    }
}
