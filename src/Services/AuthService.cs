using System;
using System.Diagnostics;
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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
        public async Task<Users> AuthenticateEmployee(string PhoneNumber, string Password)
        {
            try
            {
                var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.PhoneNumber == PhoneNumber && x.IsDeleted == false);

                if (user == null)
                    return null;

                if (user.EmployeeID > 0)
                {
                    var employees = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.EmployeeID == user.EmployeeID && x.IsDeleted == false);
                    if (employees != null)
                    {
                        user.EmployeeName = employees.EmployeeName;
                        user.DepartmentID = employees.DepartmentID;
                        user.DivisionID = employees.DivisionID;
                        user.GroupID = employees.GroupID;
                        user.CompanyID = employees.CompanyID;
                        user.Nik = employees.Nik;
                        user.StartWorkingDate = employees.StartWorkingDate;
                        user.DepartmentName = await context.Departments.AsNoTracking().Where(x => x.DepartmentID == employees.DepartmentID && x.IsDeleted == false).Select(s => s.Name ?? "").FirstOrDefaultAsync();
                        user.DivisionName = await context.Divisions.AsNoTracking().Where(x => x.DivisionID == employees.DivisionID && x.IsDeleted == false).Select(s => s.Name ?? "").FirstOrDefaultAsync();
                        user.GroupType = await context.Groups.AsNoTracking().Where(x => x.GroupID == employees.GroupID && x.IsDeleted == false).Select(s => s.Type ?? "").FirstOrDefaultAsync();
                        user.CompanyName = await context.Companies.AsNoTracking().Where(x => x.CompanyID == employees.CompanyID && x.IsDeleted == false).Select(s => s.Name ?? "").FirstOrDefaultAsync();
                    }
                }

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
        public async Task<AuthenticationOTPRequest> AuthenticateOTP(string PhoneNumber)
        {
            try
            {
                var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.PhoneNumber == PhoneNumber && x.IsDeleted == false);

                if (user == null)
                    return new AuthenticationOTPRequest { Success = false, Message = "User not found." };

                //generate OTP 
                string otp = GenerateOTP();
                user.OTP = otp;
                user.OtpExpiration = DateTime.Now.AddMinutes(2);
                user.IsVerified = false;

                context.Users.Attach(user);
                context.Entry(user).Property(x => x.OTP).IsModified = true;
                context.Entry(user).Property(x => x.OtpExpiration).IsModified = true;

                await context.SaveChangesAsync();

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
            var user = context.Users.AsNoTracking().FirstOrDefault(x => x.IsDeleted == false && x.PhoneNumber == PhoneNumber && x.OTP == code);
            try
            {
                if (user == null)
                    return null;

                // Clear password for security reasons
                user.Password = "";
                user.OTP = "";
                user.OtpExpiration = null;
                if (user.EmployeeID > 0)
                {
                    var employees = context.Employees.AsNoTracking().FirstOrDefault(x => x.EmployeeID == user.EmployeeID && x.IsDeleted == false);
                    if (employees != null)
                    {
                        user.EmployeeName = employees.EmployeeName;
                        user.DepartmentID = employees.DepartmentID;
                        user.DivisionID = employees.DivisionID;
                        user.GroupID = employees.GroupID;
                        user.CompanyID = employees.CompanyID;
                        user.Nik = employees.Nik;
                        user.StartWorkingDate = employees.StartWorkingDate;
                        user.DepartmentName = context.Departments.AsNoTracking().Where(x => x.DepartmentID == employees.DepartmentID && x.IsDeleted == false).Select(s => s.Name ?? "").FirstOrDefault();
                        user.DivisionName = context.Divisions.AsNoTracking().Where(x => x.DivisionID == employees.DivisionID && x.IsDeleted == false).Select(s => s.Name ?? "").FirstOrDefault();
                        user.GroupType = context.Groups.AsNoTracking().Where(x => x.GroupID == employees.GroupID && x.IsDeleted == false).Select(s => s.Type ?? "").FirstOrDefault();
                        user.CompanyName = context.Companies.AsNoTracking().Where(x => x.CompanyID == employees.CompanyID && x.IsDeleted == false).Select(s => s.Name ?? "").FirstOrDefault();
                    }
                }

                user.IsVerified = true;

                context.Users.Attach(user);
                context.Entry(user).Property(x => x.IsVerified).IsModified = true;
                context.SaveChanges();

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
            var user = context.Users.AsNoTracking().FirstOrDefault(x => x.Email == email && x.RoleID != 2 && x.IsDeleted == false);

            try
            {
                if (user == null)
                    return null;

                if (user.EmployeeID > 0)
                {
                    var employees = context.Employees.AsNoTracking().FirstOrDefault(x => x.EmployeeID == user.EmployeeID && x.IsDeleted == false);
                    if (employees != null)
                    {
                        user.EmployeeName = employees.EmployeeName;
                        user.DepartmentID = employees.DepartmentID;
                        user.DivisionID = employees.DivisionID;
                        user.GroupID = employees.GroupID;
                        user.CompanyID = employees.CompanyID;
                        user.Nik = employees.Nik;
                        user.StartWorkingDate = employees.StartWorkingDate;
                        user.DepartmentName = context.Departments.AsNoTracking().Where(x => x.DepartmentID == employees.DepartmentID && x.IsDeleted == false).Select(s => s.Name ?? "").FirstOrDefault();
                        user.DivisionName = context.Divisions.AsNoTracking().Where(x => x.DivisionID == employees.DivisionID && x.IsDeleted == false).Select(s => s.Name ?? "").FirstOrDefault();
                        user.GroupType = context.Groups.AsNoTracking().Where(x => x.GroupID == employees.GroupID && x.IsDeleted == false).Select(s => s.Type ?? "").FirstOrDefault();
                        user.CompanyName = context.Companies.AsNoTracking().Where(x => x.CompanyID == employees.CompanyID && x.IsDeleted == false).Select(s => s.Name ?? "").FirstOrDefault();
                    }
                }
                user.RoleName = context.Roles.Where(y => y.RoleID == user.RoleID).Select(x => x.Name ?? "").FirstOrDefault();

                user.ParentMenus = (from rd in context.RoleDetails
                                    join m in context.Modules on rd.ModuleID equals m.ModuleID
                                    where rd.RoleID == user.RoleID && m.ParentID == 0
                                    select new ParentMenu
                                    {
                                        ModuleID = rd.ModuleID,
                                        Group = m.Group,
                                        Name = m.Name,
                                        Route = m.Route,
                                        IsCreate = rd.IsCreate,
                                        IsRead = rd.IsRead,
                                        IsUpdate = rd.IsUpdate,
                                        IsDelete = rd.IsDelete
                                    }).ToList();
                user.ChildMenus = (from rd in context.RoleDetails
                                   join m in context.Modules on rd.ModuleID equals m.ModuleID
                                   where rd.RoleID == user.RoleID && m.ParentID != 0
                                   select new ChildMenu
                                   {
                                       ParentID = m.ParentID,
                                       ModuleID = rd.ModuleID,
                                       Group = m.Group,
                                       Name = m.Name,
                                       Route = m.Route,
                                       IsCreate = rd.IsCreate,
                                       IsRead = rd.IsRead,
                                       IsUpdate = rd.IsUpdate,
                                       IsDelete = rd.IsDelete
                                   }).ToList();
                user.Password = "";
                user.OTP = "";
                user.OtpExpiration = null;

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
        public string GenerateToken(Users user, int Duration)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secret = config.GetSection("AppSettings")["Secret"];
            var key = Encoding.ASCII.GetBytes(secret);
            var claims = new ClaimsIdentity(new[]
            {
                new Claim("id", user.UserID.ToString()),
				//new Claim("name", user.Name),
				new Claim("employeeid",(user?.EmployeeID ?? 0).ToString()),
                new Claim("groupid", (user?.GroupID ?? 0).ToString())
              });

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claims,
                Expires = DateTime.UtcNow.AddDays(Duration),
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
                var username = config.GetSection("Whatsapp")["username"];
                var password = config.GetSection("Whatsapp")["password"];
                var grantType = config.GetSection("Whatsapp")["grantType"];
                var clientId = config.GetSection("Whatsapp")["clientId"];
                var clientSecret = config.GetSection("Whatsapp")["clientSecret"];
                var tokenUrl = config.GetSection("Whatsapp")["tokenUrl"];

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
                        var templateId = config.GetSection("Whatsapp")["templateId"];
                        var channelId = config.GetSection("Whatsapp")["channelId"];
                        var sendMessageUrl = config.GetSection("Whatsapp")["MessageUrl"];

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