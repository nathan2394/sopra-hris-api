using System;
using Microsoft.EntityFrameworkCore;

namespace sopra_hris_api.Entities
{
    [Keyless]
    public class UserCompany
    {
        public long UserID { get; set; }
        public long EmployeeID { get; set; }
        public long RoleID { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? OtpExpiration { get; set; }
        public bool? IsVerified { get; set; }
        public string? Code { get; set; }
        public string? Company { get; set; }
        public string? ApiLink { get; set; }
        public string? LogoPath { get; set; }
    }
}