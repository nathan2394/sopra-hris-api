using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using sopra_hris_api.Entities;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

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
        public static int? TryParseNullableInt(string value)
        {
            if (string.IsNullOrEmpty(value) || value == "-")
                return null;

            if (int.TryParse(value, out int parsedValue))
                return parsedValue;

            return null;
        }
        public static decimal? TryParseNullableDecimal(string value)
        {
            if (string.IsNullOrEmpty(value) || value == "-")
                return null;

            if (decimal.TryParse(value, out decimal parsedValue))
                return parsedValue;

            return null;
        }
        public static string MaskSalary(decimal salary)
        {
            string salaryStr = salary.ToString("0");

            // Ensure salary has at least 3 digits
            if (salaryStr.Length < 3)
                return "***" + salaryStr;

            string lastThreeDigits = salaryStr.Substring(salaryStr.Length - 3);
            return "***" + lastThreeDigits;
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
        public static string HashPassword(string password)
        {
            // Generate a random salt
            //byte[] saltBytes = GenerateRandomSalt();
            string salt = BCrypt.Net.BCrypt.GenerateSalt();
            // Hash the password using PHP's password_hash equivalent
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, salt);

            return hashedPassword;
        }

        private static byte[] GenerateRandomSalt()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] salt = new byte[16];
                rng.GetBytes(salt);
                return salt;
            }
        }
        public static bool VerifyHashedPassword(string hashedPassword, string password)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // Handle the case where the salt format is not valid
                return false;
            }
        }
        public static string GetFilterValue(string key, string filter)
        {
            if (!string.IsNullOrEmpty(filter))
            {
                var filterList = filter.Split("|", StringSplitOptions.RemoveEmptyEntries);
                foreach (var f in filterList)
                {
                    var searchList = f.Split(":", StringSplitOptions.RemoveEmptyEntries);
                    if (searchList.Length == 2 && searchList[0].Trim().ToLower() == key.ToLower())
                    {
                        return searchList[1].Trim();
                    }
                }
            }
            return string.Empty;
        }
    }

}