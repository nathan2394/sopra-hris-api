
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace sopra_hris_api.Entities
{
    [Keyless]
    [Table(name: "AllowanceMeals")]
    public class AllowanceMeals : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AllowanceMealID { get; set; }
        public long EmployeeID { get; set; }
        public DateTime TransDate { get; set; }
        [NotMapped]
        public string? NIK { get; set; }
        [NotMapped]
        public string? EmployeeName { get; set; }
        [NotMapped]
        public long? DepartmentID { get; set; }
        [NotMapped]
        public string? DepartmentName { get; set; }
        [NotMapped]
        public long? DivisionID { get; set; }
        [NotMapped]
        public string? DivisionName { get; set; }
    }
    [Keyless]
    public class AllowanceMealDTO
    {
        public long EmployeeID { get; set; }
        public string NIK { get; set; }
        public string EmployeeName { get; set; }
        public DateTime TransDate { get; set; }
        public bool? Meal { get; set; }        
        public long DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
        public long? DivisionID { get; set; }
        public string? DivisionName { get; set; }
    }
}
