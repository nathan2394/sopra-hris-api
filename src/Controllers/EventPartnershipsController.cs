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
public class EventPartnershipsController : ControllerBase
{
    private readonly IServiceEventPartnershipAsync<EventPartnershipsDto> _service;

    public EventPartnershipsController(IServiceEventPartnershipAsync<EventPartnershipsDto> service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize]
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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EventPartnershipsDto obj)
    {
        try
        {
            var result = await _service.CreateAsync(obj, 88);
            var response = new Response<EventPartnershipsDto>(result);
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
            Trace.WriteLine(message, "EventPartnershipsController: Create");
            return BadRequest(new { message });
        }
    }
}
