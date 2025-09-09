using sopra_hris_api.Entities;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Entities;

namespace sopra_hris_api.src.Services
{
    public interface IAuthService
    {
        Task<AuthenticationOTPRequest> AuthenticateOTP(string PhoneNumber);
        Users AuthenticateVerifyOTP(string PhoneNumber, string Code);
        Users AuthenticateGoogle(string email);
        Task<Users> AuthenticateEmployee(string PhoneNumber, string Password);
        Task<ApplicantUsers> AuthenticateCandidate(string Email, string Password);
        string GenerateToken(Users user, int Duration);
        string GenerateTokenCandidate(ApplicantUsers user, int Duration);
    }
}
