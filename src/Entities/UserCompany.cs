using Microsoft.EntityFrameworkCore;

namespace sopra_hris_api.Entities
{
    [Keyless]
    public class UserCompany
    {
        public long UserID { get; set; }
        public string? Code { get; set; }
        public string? Company { get; set; }
        public string? ApiLink { get; set; }
        public string? LogoPath { get; set; }
    }
}