
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "EmployeeIdeas")]
    public class EmployeeIdeas : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long EmployeeIdeasID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Implementation { get; set; }
        public string Impact { get; set; }
        public string SubmissionType { get; set; }
        public string Status { get; set; }
    }
}
