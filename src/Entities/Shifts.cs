
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Shifts")]
    public class Shifts : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ShiftID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public TimeSpan? StartBufferTime { get; set; }
        public TimeSpan? EndBufferTime { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public TimeSpan? WeekendStartBufferTime { get; set; }
        public TimeSpan? WeekendEndBufferTime { get; set; }
        public TimeSpan? WeekendStartTime { get; set; }
        public TimeSpan? WeekendEndTime { get; set; }
        public long? BreakTime { get; set; }
        public int? WorkingDays { get; set; }
    }
}
