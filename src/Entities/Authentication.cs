
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    public class GoogleLoginRequest
    {
        public string Token { get; set; }
    }
    public class AuthenticationOTPRequest
    {
        public string PhoneNumber { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
    public class AuthenticationVerifyOTPRequest
    {
        public string PhoneNumber { get; set; }
        public string Code { get; set; }
    }
}
