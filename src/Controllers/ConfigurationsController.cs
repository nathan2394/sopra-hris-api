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
public class ConfigurationsController : ControllerBase
{
    private readonly IServiceAsync<Configurations> _service;

    public ConfigurationsController(IServiceAsync<Configurations> service)
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

            var response = new Response<Configurations>(result);
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
            Trace.WriteLine(message, "ConfigurationsController");
            return BadRequest(new { message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Configurations obj)
    {
        try
        {
            var result = await _service.CreateAsync(obj);
            var response = new Response<Configurations>(result);
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
            Trace.WriteLine(message, "ConfigurationsController");
            return BadRequest(new { message });
        }

    }

    [HttpPut]
    public async Task<IActionResult> Edit([FromBody] Configurations obj)
    {
        try
        {
            var result = await _service.EditAsync(obj);
            var response = new Response<Configurations>(result);
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
            Trace.WriteLine(message, "ConfigurationsController");
            return BadRequest(new { message });
        }
    }
}
