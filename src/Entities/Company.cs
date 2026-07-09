
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Company")]
    public class Company : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CompanyID { get; set; }
        public string Code { get; set; }
        public string? Name { get; set; }
        public string? ApiLink { get; set; }
        public string? LogoPath { get; set; }
    }

    public class CompanyDto
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Code { get; set; }
        public string? Company { get; set; }
        public string? ApiLink { get; set; }
        public string? LogoPath { get; set; }
        public long RoleID { get; set; }
        public string? RoleName { get; set; }
    }
}
