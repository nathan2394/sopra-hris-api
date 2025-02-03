using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using System.Collections.Generic;
using sopra_hris_api.Responses;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Services;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.src.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace sopra_hris_api.Services
{
    public class AuthService: IAuthService
    {
        private readonly EFContext context;
        private readonly IMemoryCache memoryCache;
        private readonly IConfiguration config;

        public AuthService(EFContext context, IMemoryCache memoryCache, IConfiguration config)
        {
            this.context = context;
            this.memoryCache = memoryCache;
            this.config = config;
        }

        public Users Authenticate(string PhoneNumber)
        {
            try
            {
                var user = context.Users.FirstOrDefault(x => x.PhoneNumber == PhoneNumber && x.IsDeleted == false);

                if (user == null)
                    return null;

                //if (!Helpers.Utility.VerifyHashedPassword(user.Password, request.Password))
                //    return null;

                //return response;
                DateTime utcNow = DateTime.UtcNow; // Get the current UTC time
                TimeZoneInfo gmtPlus7 = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Time zone for GMT+7
                DateTime gmtPlus7Time = TimeZoneInfo.ConvertTimeFromUtc(utcNow, gmtPlus7); // Convert UTC to GMT+7

                user.Password = "";

                return user;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                return null;
            }
            finally
            {
                context.Dispose();
            }
        }
        public Users AuthenticateGoogle(string email)
        {
            var user = context.Users.FirstOrDefault(x => x.Email == email && x.IsDeleted == false);

            try
            {
                if (user == null)
                    return null;

                //return response;
                DateTime utcNow = DateTime.UtcNow; // Get the current UTC time
                TimeZoneInfo gmtPlus7 = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Time zone for GMT+7
                DateTime gmtPlus7Time = TimeZoneInfo.ConvertTimeFromUtc(utcNow, gmtPlus7); // Convert UTC to GMT+7

                user.Password = "";

                return user;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                return null;
            }
            finally
            {
                context.Dispose();
            }
        }
        public string GenerateToken(Users user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secret = config.GetSection("AppSettings")["Secret"];
            var key = Encoding.ASCII.GetBytes(secret);
            var claims = new ClaimsIdentity(new[]
            {
                new Claim("id", user.UserID.ToString()),
				//new Claim("name", user.Name),
				new Claim("roleid", user.RoleID.ToString())
              });

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claims,
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateOTP()
        {
            // Generate a random 4-digit OTP
            Random random = new Random();
            int otp = random.Next(1000, 9999);
            return otp.ToString();
        }

        private string FormatPhoneNumber(string number)
        {
            //if (number.StartsWith("0")) return number;
            //         else if (number.StartsWith("62")) return number;
            //         else if (!number.StartsWith("0") && !number.StartsWith("62") && number.StartsWith("8")) return number;

            number = number.TrimStart('0');

            if (!number.StartsWith("62"))
            {
                number = "62" + number;
            }

            return number;
        }

        // Define a class to represent the token response
        public class TokenResponse
        {
            public string access_token { get; set; }
        }
    }
}