
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
    public class ApplicantUsers
    {
        public long ApplicantID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string MobilePhoneNumber { get; set; }
        public string Password { get; set; }
    }
    public class SendOtpRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class VerifyOtpRequest
    {
        public string Email { get; set; }
        public string OTPCode { get; set; }
    }
}
