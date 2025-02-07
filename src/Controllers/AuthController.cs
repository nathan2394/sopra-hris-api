using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using sopra_hris_api.Entities;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Services;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Numerics;

namespace sopra_hris_api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;
    private readonly IServiceAsync<Users> _userService;
    private readonly HttpClient _httpClient;

    public AuthController(IAuthService service, IServiceAsync<Users> userService)
    {
        _service = service;
        _userService = userService;
        _httpClient = new HttpClient();

    }

    [HttpPost("login")]
    public async Task<IActionResult> Authenticate([FromQuery] string PhoneNumber)
    {
        try
        {
            var result = await _service.AuthenticateOTP(PhoneNumber);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = "OTP sent successfully" });
        }
        catch (Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                message = inner.Message;
                inner = inner.InnerException;
            }
            Trace.WriteLine(message, "AuthController");
            return BadRequest(new { message });
        }
    }
    [HttpPost("validate-otp")]
    public IActionResult ValidateOtp([FromQuery] AuthenticationVerifyOTPRequest request)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrEmpty(request.Code))
                return BadRequest(new { message = "Code cannot be empty" });

            var user = _service.AuthenticateVerifyOTP(request.PhoneNumber, request.Code);
            if (user == null)
                return Unauthorized(new { message = "Code OTP is incorrect" });

            if (user.OtpExpiration.HasValue && (user.OtpExpiration.Value - DateTime.Now).TotalMinutes < 0)
                return BadRequest(new { message = "OTP is expired" });

            // Generate a token for the authenticated user
            var token = _service.GenerateToken(user);
            var response = new AuthResponse(user, token);

            return Ok(response);
        }
        catch (Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                message = inner.Message;
                inner = inner.InnerException;
            }
            Trace.WriteLine(message, "AuthController");
            return BadRequest(new { message });
        }
    }
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        try
        {

            if (string.IsNullOrEmpty(request.Token))
                return BadRequest("Token is required.");

            // Verify the token with Google
            var googleApiUrl = $"https://www.googleapis.com/oauth2/v1/userinfo?access_token={request.Token}";
            var googleresponse = await _httpClient.GetAsync(googleApiUrl);

            if (!googleresponse.IsSuccessStatusCode)
                return Unauthorized("Invalid Google token.");

            var jsonResponse = JObject.Parse(await googleresponse.Content.ReadAsStringAsync());

            // Extract user information from the token
            var email = jsonResponse["email"]?.ToString();
            var name = jsonResponse["name"]?.ToString();

            if (string.IsNullOrEmpty(email))
                return Unauthorized("Failed to retrieve user information.");

            // You can add custom logic here, like creating a user in your database

            var user = _service.AuthenticateGoogle(email);
            if (user == null)
                return BadRequest(new { message = "Email is not registered" });

            var token = _service.GenerateToken(user);
            var response = new AuthResponse(user, token);

            return Ok(response);
        }
        catch (Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                message = inner.Message;
                inner = inner.InnerException;
            }
            Trace.WriteLine(message, "AuthController");
            return BadRequest(new { message });
        }
    }
}
