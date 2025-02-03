
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
        public string OTP { get; set; }
    }
}
