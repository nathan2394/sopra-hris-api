using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using sopra_hris_api.Entities;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Entities;
using sopra_hris_api.src.Services;

namespace sopra_hris_api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class AttendancesController : ControllerBase
{
    private readonly IServiceAttendancesAsync<Attendances> _service;

    public AttendancesController(IServiceAttendancesAsync<Attendances> service)
    {
        _service = service;
    }

    [HttpGet("{date}")]
    public async Task<IActionResult> Get(string date = "", string filter = "")
    {
        try
        {
            var result = await _service.GetAllAsync(filter, date);
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
    [HttpGet("ListAttendance/{employeeid}/{date}")]
    public async Task<IActionResult> GetListAttendance(long employeeid, string date)
    {
        try
        {
            var result = await _service.GetAllAsync(0, 0, 0, employeeid, date);
            if (result == null)
                return BadRequest(new { message = "Invalid ID" });
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
    [HttpGet("DetailAttendance/{employeeid}/{date}")]
    public async Task<IActionResult> GetById(long employeeid, string date)
    {
        try
        {
            var result = await _service.GetDetailAsync(employeeid, date);
            if (result == null)
                return BadRequest(new { message = "Invalid ID" });

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
            Trace.WriteLine(message, "AttendancesController");
            return BadRequest(new { message });
        }
    }

    [HttpGet("AttendanceShift/{employeeid}/{date}")]
    public async Task<IActionResult> GetShifts(long employeeid, string date)
    {
        try
        {
            var result = await _service.GetDetailShiftsAsync(employeeid, date);
            if (result == null)
                return BadRequest(new { message = "Invalid ID" });

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
            Trace.WriteLine(message, "AttendancesController");
            return BadRequest(new { message });
        }
    }
    [HttpPost("SaveAttendances")]
    public async Task<IActionResult> SaveAttendances([FromBody] AttendanceDTO attendance)
    {
        try
        {
            var result = await _service.SaveAttendancesAsync(attendance);
            var response = new Response<AttendanceDetails>(result);
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
            Trace.WriteLine(message, "AttendancesController");
            return BadRequest(new { message });
        }

    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Attendances obj)
    {
        try
        {
            obj.UserIn = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.CreateAsync(obj);
            var response = new Response<Attendances>(result);
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
            Trace.WriteLine(message, "AttendancesController");
            return BadRequest(new { message });
        }

    }

    [HttpPut]
    public async Task<IActionResult> Edit([FromBody] Attendances obj)
    {
        try
        {
            obj.UserUp = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.EditAsync(obj);
            var response = new Response<Attendances>(result);
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
            Trace.WriteLine(message, "AttendancesController");
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
            Trace.WriteLine(message, "AttendancesController");
            return BadRequest(new { message });
        }
    }
    [HttpPost]
    [Route("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var allowedExtensions = new[] { ".pdf", ".jpeg", ".jpg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest("format files are not allowed.");

            // Save the file to a folder
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "AttachmentFiles");
            var customFileName = $"{Guid.NewGuid()}_{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";

            var filePath = Path.Combine(folderPath, customFileName);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            var relativeFilePath = Path.Combine("AttachmentFiles", customFileName);
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
}
