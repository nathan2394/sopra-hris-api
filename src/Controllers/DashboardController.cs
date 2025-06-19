using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sopra_hris_api.Entities;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Entities;
using sopra_hris_api.src.Services;

namespace sopra_hris_api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IServiceDashboardAsync<DashboardDTO> _service;

    public DashboardController(IServiceDashboardAsync<DashboardDTO> service)
    {
        _service = service;
    }

    [HttpGet("Approval")]
    public async Task<IActionResult> GetApproval(string filter = "", string date = "")
    {
        try
        {
            var result = await _service.GetApproval(filter, date);
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
    [HttpGet("summary")]
    public async Task<IActionResult> GetAttendanceSummary(string filter = "", string date = "")
    {
        try
        {
            var result = await _service.GetAttendanceSummary(filter, date);
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
    [HttpGet("AttendanceNormalAbnormal")]
    public async Task<IActionResult> GetAttendanceNormalAbnormal(string filter = "", string date = "")
    {
        try
        {
            var result = await _service.GetAttendanceNormalAbnormal(filter, date);
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
    [HttpGet("AttendanceByShift")]
    public async Task<IActionResult> GetAttendanceByShift(string filter = "", string date = "")
    {
        try
        {
            var result = await _service.GetAttendanceByShift(filter, date);
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
    [HttpGet("BudgetOvertimes")]
    public async Task<IActionResult> GetBudgetOvertimes(string filter = "", string date = "")
    {
        try
        {
            var result = await _service.GetBudgetOvertimes(filter, date);
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
}
