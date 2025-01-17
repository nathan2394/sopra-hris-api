﻿using sopra_hris_api.Entities;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Entities;

namespace sopra_hris_api.src.Services
{
    public interface IAuthService
    {
        Users Authenticate(string email, string password);
        Users AuthenticateGoogle(string email);
        string GenerateToken(Users user);
    }
}
