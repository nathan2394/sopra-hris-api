using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sopra_hris_api.Entities;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Services;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Numerics;
using sopra_hris_api.Helpers;

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
    public async Task<IActionResult> Authenticate([FromQuery] string PhoneNumber, [FromQuery] string Password)
    {
        try
        {
            var user = _service.AuthenticateByKey(PhoneNumber, null, false);
            if (user == null)
                return NotFound(new { message = "User not found" });

            if (!Utility.VerifyHashedPassword(user.Password, Password))
                return Unauthorized(new { message = "Incorrect password" });

            var message = "";
            if (!user.IsVerified.HasValue || !user.IsVerified.Value)
                message = "Not verified";

            user.Password = "";
            user.OTP = "";
            user.OtpExpiration = null;

            message = "Login successful";

            var token = message == "Not verified" ? null : _service.GenerateToken(user, 1);
            var response = new AuthResponse(user, token, message);

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

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromQuery] string PhoneNumber)
    {
        try
        {
            var result = await _service.AuthenticateOTP(PhoneNumber);
            if (result.Success)
                return Ok(new { message = "OTP sent successfully" });
            else
                return BadRequest(new { message = result.Message });
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
            var token = _service.GenerateToken(user, 1);
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

            var user = _service.AuthenticateByKey(null, email, false);
            if (user == null)
                return BadRequest(new { message = "Email is not registered" });
            
            var token = _service.GenerateToken(user, 7);
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

    [Authorize]
    [HttpGet("refresh-user")]
    public async Task<IActionResult> RefreshUser()
    {
        try
        {
           var userEmail = User.FindFirstValue("email");

            if (string.IsNullOrEmpty(userEmail.ToString()))
            {
                return BadRequest(new { message = "User email not found in claims" });
            }

            var user = _service.AuthenticateByKey(null, userEmail.ToString(), true);
            if (user == null)
            {
                return BadRequest(new { message = "User not found" });
            }

            // Generate JWT token using existing UserService
            var token = _service.GenerateToken(user, 1);
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
            Trace.WriteLine(message, "AuthController: RefreshUser");
            return BadRequest(new { message });
        }
    }

    [HttpPost("loginCandidate")]
    public async Task<IActionResult> AuthenticateCandidate([FromQuery] string Email, [FromQuery] string Password)
    {
        try
        {
            var user = await _service.AuthenticateCandidate(Email, Password);
            if (user == null)
                return NotFound(new { message = "User not found" });

            if (!Utility.VerifyHashedPassword(user.Password, Password))
                return Unauthorized(new { message = "Incorrect password" });

            //if (!user.IsVerified.HasValue || !user.IsVerified.Value)
            //{
            //    var result = await _service.AuthenticateOTP(Email);
            //    if (result.Success)
            //        return Ok(new { message = "OTP sent successfully" });
            //    else
            //        return BadRequest(new { message = result.Message });
            //}
            user.Password = "";

            var token = _service.GenerateTokenCandidate(user, 1);
            var response = new AuthResponseCandidate(user, token);

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
