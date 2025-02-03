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
    public IActionResult Authenticate([FromQuery] string PhoneNumber)
    {
        try
        {
            var user = _service.Authenticate(PhoneNumber);
            if (user == null)
                return BadRequest(new { message = "Phone Number is incorrect" });

            //var token = _service.GenerateToken(user);
            //var response = new AuthResponse(user, token);

            //return Ok(response);

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
    public IActionResult ValidateOtp([FromQuery] AuthenticationOTPRequest request)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.OTP))
                return BadRequest(new { message = "Phone number and OTP are required" });

            var user = _service.Authenticate(request.PhoneNumber);
            if (user == null)
                return BadRequest(new { message = "Phone Number is incorrect" });

            if (user.OTP != request.OTP)
                return BadRequest(new { message = "Invalid OTP" });

            if (user.OtpExpiration >= DateTime.Now)
                return BadRequest(new { message = "OTP has expired!" });

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
