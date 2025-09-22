using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sopra_hris_api.Entities;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Services;

namespace sopra_hris_api.Controllers;

[ApiController]
[Route("[controller]")]
public class ApplicantsController : ControllerBase
{
    private readonly IServiceApplicantAsync<Applicants> _service;

    public ApplicantsController(IServiceApplicantAsync<Applicants> service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get(int limit = 0, int page = 0, string search = "", string sort = "", string filter = "", string date = "")
    {
        try
        {
            var total = 0;
            var result = await _service.GetAllAsync(limit, page, total, search, sort, filter, date);
            return Ok(result);
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
            return BadRequest(new { message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return BadRequest(new { message = "Invalid ID" });

            var response = new Response<Applicants>(result);
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
            return BadRequest(new { message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Applicants obj)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(obj.FullName) ||
                string.IsNullOrWhiteSpace(obj.Email) ||
                string.IsNullOrWhiteSpace(obj.MobilePhoneNumber) ||
                obj.CandidateID == null || obj.CandidateID == 0)
            {
                return BadRequest(new { message = "Name, Email, PhoneNumber are required." });
            }
            obj.UserIn = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.CreateAsync(obj);
            var response = new Response<Applicants>(result);
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
            return BadRequest(new { message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> Edit([FromBody] Applicants obj)
    {
        try
        {
            obj.UserUp = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.EditAsync(obj);
            var response = new Response<Applicants>(result);
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
            return BadRequest(new { message });
        }
    }
    [HttpPut("ChangePassword")]
    public async Task<IActionResult> ChangePassword([FromBody] ApplicantChangePassword obj)
    {
        try
        {
            obj.UserUp = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.ChangePasswordAsync(obj);
            var response = new Response<Applicants>(result);
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
            return BadRequest(new { message });
        }
    }
    [HttpPost("ForgotPassword")]
    public async Task<IActionResult> ForgotPassword(string Email)
    {
        try
        {
            var result = await _service.SendForgotPasswordOTPAsync(Email);

            if (result == "OTP sent successfully.")
                return Ok(new { message = result });

            return BadRequest(new { message = result });
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
            return BadRequest(new { message });
        }
    }
    [HttpPost("ResetPassword")]
    public async Task<IActionResult> VerifyOTPAndResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var result = await _service.VerifyOTPAndResetPasswordAsync(request);

            if (result == "Password has been reset successfully.")
                return Ok(new { message = result });

            return BadRequest(new { message = result });
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
            Trace.WriteLine(message, "ApplicationsController");
            return BadRequest(new { message });
        }
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _service.DeleteAsync(id, Convert.ToInt64(User.FindFirstValue("id")));

            var response = new Response<object>(result);
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
            return BadRequest(new { message });
        }
    }

    [HttpPost("{applicantId}/{Percentage}")]
    public async Task<IActionResult> MarkAsCompleted(long applicantId, int Percentage)
    {
        try
        {
            var result = await _service.ProfileCompletion(applicantId, Percentage);
            return Ok(new {result});
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
            return BadRequest(new { message });
        }
    }
    [HttpGet("percentage/{applicantId}")]
    public async Task<IActionResult> GetPercentage(long applicantId)
    {
        try
        {
            var result = await _service.GetCompletionAsync(applicantId);
            if (result == null)
                return BadRequest(new { message = "Invalid ID" });
            return Ok(new { result });
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
            return BadRequest(new { message });
        }
    }
}
