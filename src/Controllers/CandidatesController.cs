using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sopra_hris_api.Entities;
using sopra_hris_api.Responses;
using sopra_hris_api.Services;
using sopra_hris_api.src.Services;

namespace sopra_hris_api.Controllers;

[ApiController]
[Route("[controller]")]
public class CandidatesController : ControllerBase
{
    private readonly IServiceJobsAsync<Candidates> _service;

    public CandidatesController(IServiceJobsAsync<Candidates> service)
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

            var response = new Response<Candidates>(result);
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
            Trace.WriteLine(message, "CandidatesController");
            return BadRequest(new { message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Candidates obj)
    {
        try
        {
            //if (obj.OTPCode == null)
            //{
            //    return BadRequest(new { message = "OTP Code is required." });
            //}

            if (string.IsNullOrWhiteSpace(obj.CandidateName) ||
                string.IsNullOrWhiteSpace(obj.Email) ||
                string.IsNullOrWhiteSpace(obj.PhoneNumber) ||
                string.IsNullOrWhiteSpace(obj.ResumeURL))
            {
                return BadRequest(new { message = "CandidateName, Email, PhoneNumber, and ResumeUrl are required." });
            }

            //var checkotp = await _service.VerifyOTP(obj.Email, obj.OTPCode);
            //if (checkotp)
            //    obj.OtpVerify = true;
            //else
            //    return BadRequest("Invalid OTP or OTP has expired.");

            obj.OtpVerify = true;
            obj.UserIn = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.CreateAsync(obj);
            var response = new Response<Candidates>(result);
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
            Trace.WriteLine(message, "CandidatesController");
            return BadRequest(new { message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> Edit([FromBody] Candidates obj)
    {
        try
        {
            obj.UserUp = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.EditAsync(obj);
            var response = new Response<Candidates>(result);
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
            Trace.WriteLine(message, "CandidatesController");
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
            Trace.WriteLine(message, "CandidatesController");
            return BadRequest(new { message });
        }
    }
    [HttpPost]
    [Route("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        try
        {
            long MaxFileSize = 2 * 1024 * 1024;
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            if (file.Length > MaxFileSize)
            {
                return BadRequest($"File size exceeds the allowed limit of {MaxFileSize / (1024 * 1024)}MB.");
            }

            var allowedExtensions = new[] { ".pdf" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest("format files are not allowed.");

            // Save the file to a folder
            var customFileName = $"{Guid.NewGuid()}_{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";

            var today = DateTime.Now;
            var datePath = Path.Combine(today.Year.ToString(), today.Month.ToString("D2"), today.Day.ToString("D2"));

            var baseFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "ApplicationsFiles");
            var folderPath = Path.Combine(baseFolderPath, datePath);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, customFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            var relativeFilePath = Path.Combine("ApplicationsFiles", datePath, customFileName).Replace("\\", "/");
            return Ok(new { AttachmentPath = relativeFilePath });
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
    [HttpPost("SendEmailAndGenerateOTP")]
    public async Task<IActionResult> SendEmailAndGenerateOTP([FromBody] SendOtpRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { message = "Email is required." });

            // Send OTP via Email
            var result = await _service.SaveOTPToDatabase(request.Name, request.Email);

            if (result == "OTP has been sent to your email.")
                return Ok(new { message = result });
            else if (result == "Account found.")
                return StatusCode(409, new { message = result });

            return StatusCode(500, new { message = result });
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
            Trace.WriteLine(message, "CandidatesController");
            return BadRequest(new { message });
        }
    }
    [HttpPost("VerifyOTP")]
    public async Task<IActionResult> VerifyOTP([FromBody] VerifyOtpRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { message = "Email is required." });

            if (string.IsNullOrWhiteSpace(request.OTPCode))
                return BadRequest(new { message = "OTP Code is required." });


            var isValid = await _service.VerifyOTP(request.Email, request.OTPCode);
            if (!isValid)
                return BadRequest(new { message = "Invalid OTP or OTP has expired." });

            return Ok(new { message = "Email verified successfully." });
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
            Trace.WriteLine(message, "CandidatesController");
            return BadRequest(new { message });
        }
    }
}
