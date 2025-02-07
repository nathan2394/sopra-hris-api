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
using System.Text.Json;
using System.Numerics;

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

        public async Task<AuthenticationOTPRequest> AuthenticateOTP(string PhoneNumber)
        {
            try
            {
                var user = context.Users.FirstOrDefault(x => x.PhoneNumber == PhoneNumber && x.IsDeleted == false);

                if (user == null)
                    return new AuthenticationOTPRequest { Success = false, Message = "User not found." };

                //generate OTP 
                string otp = GenerateOTP();
                user.OTP = otp;
                user.OtpExpiration = DateTime.Now.AddMinutes(2);
                context.SaveChanges();

                string userName = user.Name;
                // Send OTP via WhatsApp
                await SendOTP(userName, PhoneNumber, otp);

                return new AuthenticationOTPRequest { Success = true, Message = "OTP sent successfully." };
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                return new AuthenticationOTPRequest { Success = false, Message = "Error occurred while processing the request." };
            }
            finally
            {
                context.Dispose();
            }
        }
        public Users AuthenticateVerifyOTP(string PhoneNumber, string code)
        {
            var user = context.Users.FirstOrDefault(x => x.IsDeleted == false && x.PhoneNumber == PhoneNumber && x.OTP == code);
            try
            {
                if (user == null)
                    return null;

                if (user.OtpExpiration.HasValue && (user.OtpExpiration.Value - DateTime.Now).TotalMinutes < 0)
                    return null;

                // Clear password for security reasons
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
        private async Task SendOTP(string name, string number, string otp)
        {
            try
            {
                // Format phone number
                var formattedNumber = FormatPhoneNumber(number);

                // Prepare authentication data
                var username = "dgtmkt@solusi-pack.com";
                var password = "Admin123!";
                var grantType = "password";
                var clientId = "RRrn6uIxalR_QaHFlcKOqbjHMG63elEdPTair9B9YdY";
                var clientSecret = "Sa8IGIh_HpVK1ZLAF0iFf7jU760osaUNV659pBIZR00";
                var tokenUrl = "https://service-chat.qontak.com/api/open/v1/oauth/token";

                // Prepare HttpClient instance
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(tokenUrl);

                    // Prepare request body
                    var requestBody = new
                    {
                        username,
                        password,
                        grant_type = grantType,
                        client_id = clientId,
                        client_secret = clientSecret
                    };

                    // Convert request body to JSON
                    var jsonRequest = JsonSerializer.Serialize(requestBody);

                    // Prepare request content
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                    // Send token request
                    var tokenResponse = await client.PostAsync(tokenUrl, content);

                    // Check if token request was successful
                    if (tokenResponse.IsSuccessStatusCode)
                    {
                        // Read token response
                        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                        var token = JsonSerializer.Deserialize<TokenResponse>(tokenContent);

                        // Prepare OTP message data
                        var templateId = "7bc159f1-349d-4ede-859c-a4b61ed6cb73";
                        var channelId = "1a354b7e-d46b-470b-a8e7-9e5841e48b1b";
                        var sendMessageUrl = "https://service-chat.qontak.com/api/open/v1/broadcasts/whatsapp/direct";

                        var messageBody = new
                        {
                            to_name = name,
                            to_number = formattedNumber,
                            message_template_id = templateId,
                            channel_integration_id = channelId,
                            language = new { code = "en" },
                            parameters = new
                            {
                                body = new[]
                                {
                                    new { key = "1", value_text = otp, value = "10" }
                                },
                                buttons = new[]
                                {
                                    new { index = "0", type = "URL", value = otp }
                                }
                            }
                        };

                        // Convert message body to JSON
                        var jsonMessageBody = JsonSerializer.Serialize(messageBody);

                        // Prepare request content for sending message
                        var sendMessageContent = new StringContent(jsonMessageBody, Encoding.UTF8, "application/json");

                        var requestMessage = new HttpRequestMessage(HttpMethod.Post, sendMessageUrl);
                        requestMessage.Content = sendMessageContent;
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.access_token);

                        // Send OTP message
                        var sendMessageResponse = await client.SendAsync(requestMessage);

                        // Check if OTP message was sent successfully
                        if (sendMessageResponse.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"{name} Send OTP code to {number}");
                            // Log success or handle as needed
                        }
                        else
                        {
                            Console.WriteLine($"{name} Failed Send OTP code to {number}. StatusCode: {sendMessageResponse.StatusCode}");
                            // Log failure or handle as needed
                            throw new Exception($"{name} Failed Send OTP code to {number}. StatusCode: {sendMessageResponse.StatusCode}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Failed to retrieve access token.");
                        // Log failure or handle as needed
                        throw new Exception("Failed to retrieve access token.");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while sending OTP: {ex.Message}");
                // Log any exceptions
                throw new Exception($"An error occurred while sending OTP: {ex.Message}");
            }
        }
        private string FormatPhoneNumber(string number)
        {
            number = number.TrimStart('0');

            if (!number.StartsWith("62"))
                number = "62" + number;

            return number;
        }

        // Define a class to represent the token response
        public class TokenResponse
        {
            public string access_token { get; set; }
        }
    }
}