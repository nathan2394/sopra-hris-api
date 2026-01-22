using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using sopra_hris_api.Entities;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Services;
using sopra_hris_api.src.Services.API;

namespace sopra_hris_api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class PerformanceTemplatesController : ControllerBase
{
    private readonly IServicePerformanceTemplateAsync<PerformanceTemplates> _service;

    public PerformanceTemplatesController(IServicePerformanceTemplateAsync<PerformanceTemplates> service)
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

            var response = new Response<PerformanceTemplatesDto>(result);
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
            Trace.WriteLine(message, "PerformanceTemplatesController: Get By ID");
            return BadRequest(new { message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PerformanceTemplatesDto obj)
    {
        try
        {
            var userID = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.CreateAsync(obj, userID);
            var response = new Response<PerformanceTemplates>(result);
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
            Trace.WriteLine(message, "PerformanceTemplatesController: Create");
            return BadRequest(new { message });
        }

    }

    [HttpPut]
    public async Task<IActionResult> Edit([FromBody] PerformanceTemplatesDto obj)
    {
        try
        {
            var userID = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.EditAsync(obj, userID);
            var response = new Response<PerformanceTemplates>(result);
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
            Trace.WriteLine(message, "PerformanceTemplatesController: Edit");
            return BadRequest(new { message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userID = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.DeleteAsync(id, userID);
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
            Trace.WriteLine(message, "PerformanceTemplatesController: Delete");
            return BadRequest(new { message });
        }
    }
}
