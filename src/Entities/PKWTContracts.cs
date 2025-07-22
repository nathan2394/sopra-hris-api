
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "PKWTContracts")]
    public class PKWTContracts : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PWKTID { get; set; }
        public long EmployeeID { get; set; }
        public string? PKWTNo { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? ContractType { get; set; }
        public DateTime? LaidOffDate { get; set; }
        public DateTime? LaidOffEndDate { get; set; }
        public string? Remarks { get; set; }
    }
}
