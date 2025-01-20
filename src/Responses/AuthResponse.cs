using sopra_hris_api.Entities;

namespace sopra_hris_api.Responses
{
    public class AuthResponse : Response<Users>
    {
        public string Token { get; set; }

        public AuthResponse(Users user, string token)
        {
            Token = token;
            Data = user;
        }
    }
}


