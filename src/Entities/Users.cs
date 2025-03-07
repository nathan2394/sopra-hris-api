
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Users")]
    public class Users : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserID { get; set; }
        public long EmployeeID { get; set; }
        public long RoleID { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? PhoneNumber { get; set; }
        public string? OTP { get; set; }
        public DateTime? OtpExpiration { get; set; }
        public bool? IsVerified { get; set; }
        [NotMapped]
        public string? EmployeeName { get; set; }
        [NotMapped]
        public string? Nik { get; set; }
        [NotMapped]
        public DateTime? StartWorkingDate{ get; set; }
        [NotMapped]
        public long? DepartmentID { get; set; }
        [NotMapped]
        public long? DivisionID { get; set; }
        [NotMapped]
        public long? CompanyID { get; set; }
        [NotMapped]
        public long? GroupID { get; set; }
        [NotMapped]
        public string? DepartmentName { get; set; }
        [NotMapped]
        public string? DivisionName { get; set; }
        [NotMapped]
        public string? CompanyName { get; set; }
        [NotMapped]
        public string? GroupType { get; set; }
        [NotMapped]
        public string? RoleName { get; set; }        
        [NotMapped]
        public List<ParentMenu>? ParentMenus { get; set; }
        [NotMapped]
        public List<ChildMenu>? ChildMenus { get; set; }
    }
    public class ParentMenu
    {
        public long ModuleID { get; set; }
        public string Group { get; set; }
        public string Name { get; set; }
        public string Route { get; set; }
        public long IsCreate { get; set; }
        public long IsRead { get; set; }
        public long IsUpdate { get; set; }
        public long IsDelete { get; set; }
    }
    public class ChildMenu
    {
        public long ParentID { get; set; }
        public long ModuleID { get; set; }
        public string Group { get; set; }
        public string Name { get; set; }
        public string Route { get; set; }
        public long IsCreate { get; set; }
        public long IsRead { get; set; }
        public long IsUpdate { get; set; }
        public long IsDelete { get; set; }
    }
}
