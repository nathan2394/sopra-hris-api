using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Entities;
using sopra_hris_api.src.Services;

namespace sopra_hris_api.Controllers;

[ApiController]
[Route("[controller]")]
public class SalaryDetailsController : ControllerBase
{
    private readonly IServiceSalaryDetailsAsync<SalaryDetails> _service;

    public SalaryDetailsController(IServiceSalaryDetailsAsync<SalaryDetails> service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get(string filter = "")
    {
        try
        {
            var result = await _service.GetSalaryDetailReports(filter);
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
    public async Task<IActionResult> GetById(long id)
    {
        try
        {
            var result = await _service.GetSalaryDetails(id);
            if (result == null)
                return BadRequest(new { message = "Invalid ID" });

            var response = new Response<SalaryDetailReportsDTO>(result);
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
            Trace.WriteLine(message, "SalaryDetailsController");
            return BadRequest(new { message });
        }
    }
}
