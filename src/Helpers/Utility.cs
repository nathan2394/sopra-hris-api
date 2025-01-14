using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using sopra_hris_api.Entities;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace sopra_hris_api.Helpers
{
    public static class Utility
    {
        private static IConfiguration config;
        public static IConfiguration Configuration
        {
            get
            {
                if (config == null)
                {
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json");
                    config = builder.Build();
                }
                return config;
            }
        }

        public static string Secret { get { return Configuration.GetSection("AppSettings:Secret").Value; } }
        public static DateTime getCurrentTimestamps()
        {
            DateTime utcNow = DateTime.UtcNow; // Get the current UTC time
            TimeZoneInfo gmtPlus7 = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Time zone for GMT+7
            DateTime gmtPlus7Time = TimeZoneInfo.ConvertTimeFromUtc(utcNow, gmtPlus7); // Convert UTC to GMT+7
            return gmtPlus7Time;
        }
        public static Users UserFromToken(string token)
        {
            try
            {
                //var secret = config.GetSection("AppSettings")["Secret"];
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(Secret);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var id = long.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);

                return null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                return null;
            }
        }
    }

}