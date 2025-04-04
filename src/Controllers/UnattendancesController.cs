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
[Authorize]
public class UnattendancesController : ControllerBase
{
    private readonly IServiceUnAttendancesAsync<Unattendances> _service;

    public UnattendancesController(IServiceUnAttendancesAsync<Unattendances> service)
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

            var response = new Response<Unattendances>(result);
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
            Trace.WriteLine(message, "UnattendancesController");
            return BadRequest(new { message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Unattendances obj)
    {
        try
        {
            obj.UserIn = Convert.ToInt64(User.FindFirstValue("id"));

            // Handle file uploads
            //if (attachments != null && attachments.Any())
            //{
            //    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "UnattendanceUploads");
            //    if (!Directory.Exists(uploadsFolder))
            //        Directory.CreateDirectory(uploadsFolder);
                
            //    obj.UnattendanceAttachments = new List<UnattendanceAttachments>();

            //    foreach (var file in attachments)
            //    {
            //        var filePath = Path.Combine(uploadsFolder, file.FileName);
            //        using (var stream = new FileStream(filePath, FileMode.Create))
            //        {
            //            await file.CopyToAsync(stream);
            //        }

            //        obj.UnattendanceAttachments.Add(new UnattendanceAttachments
            //        {
            //            FileName = file.FileName,
            //            FilePath = filePath
            //        });
            //    }
            //}

            var result = await _service.CreateAsync(obj);
            var response = new Response<Unattendances>(result);
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
            Trace.WriteLine(message, "UnattendancesController");
            return BadRequest(new { message });
        }

    }

    [HttpPut]
    public async Task<IActionResult> Edit([FromBody] Unattendances obj)
    {
        try
        {
            obj.UserUp = Convert.ToInt64(User.FindFirstValue("id"));
            //// Handle file uploads
            //if (attachments != null && attachments.Any())
            //{
            //    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "UnattendanceUploads");
            //    if (!Directory.Exists(uploadsFolder))
            //        Directory.CreateDirectory(uploadsFolder);

            //    obj.UnattendanceAttachments = new List<UnattendanceAttachments>();

            //    foreach (var file in attachments)
            //    {
            //        var filePath = Path.Combine(uploadsFolder, file.FileName);
            //        using (var stream = new FileStream(filePath, FileMode.Create))
            //        {
            //            await file.CopyToAsync(stream);
            //        }

            //        obj.UnattendanceAttachments.Add(new UnattendanceAttachments
            //        {
            //            FileName = file.FileName,
            //            FilePath = filePath
            //        });
            //    }
            //}
            var result = await _service.EditAsync(obj);
            var response = new Response<Unattendances>(result);
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
            Trace.WriteLine(message, "UnattendancesController");
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
            Trace.WriteLine(message, "UnattendancesController");
            return BadRequest(new { message });
        }
    }
}
