using sopra_hris_api.Entities;

namespace sopra_hris_api.Responses
{
    public class AuthResponse : Response<Users>
    {
        public string Token { get; set; }
        public string Message { get; set; }

        public AuthResponse(Users user, string token, string message = "")
        {
            Token = token;
            Data = user;
            Message = message;
        }
    }
    public class AuthResponseCandidate : Response<ApplicantUsers>
    {
        public string Token { get; set; }

        public AuthResponseCandidate(ApplicantUsers user, string token)
        {
            Token = token;
            Data = user;
        }
    }
}


