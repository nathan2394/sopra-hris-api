
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Metrics;

namespace sopra_hris_api.Entities
{
    [Table(name: "MatrixApproval")]
    public class MatrixApproval : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long MatrixApprovalID { get; set; }
        public long DepartmentID { get; set; }
        public long? DivisionID { get; set; }
        public long? Maker { get; set; }
        public long? Checker { get; set; }
        public long? Releaser { get; set; }
    }
}
