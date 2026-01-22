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
public class PerformanceEmployeeReviewerController : ControllerBase
{
    private readonly IServicePerformanceEmployeeReviewerAsync<PerformanceEmployeeReviewers> _service;

    public PerformanceEmployeeReviewerController(IServicePerformanceEmployeeReviewerAsync<PerformanceEmployeeReviewers> service)
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

    [HttpGet("EmployeeForm/{id}")]
    public async Task<IActionResult> GetEmployeeFormById(long id)
    {
        try
        {
            var reviewerID = Convert.ToInt64(User.FindFirstValue("employeeid"));

            var result = await _service.GetEmployeeFormByIdAsync(id, reviewerID);
            var response = new Response<ReviewerFormsDto>(result);
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
            Trace.WriteLine(message, "PerformanceEmployeeReviewerController: Get By Employee ID");
            return BadRequest(new { message });
        }
    }

    [HttpGet("EmployeeList")]
    public async Task<IActionResult> GetEmployeeListById()
    {
        try
        {
            var userID = Convert.ToInt64(User.FindFirstValue("employeeid"));

            var result = await _service.GetEmployeeListByIdAsync(userID);
            var response = new Response<List<ToBeReviewedEmployeesDto>>(result);
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
            Trace.WriteLine(message, "PerformanceEmployeeReviewerController: Get By Employee ID");
            return BadRequest(new { message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> Edit([FromBody] ReviewerFormsDto obj)
    {
        try
        {
            var userID = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.EditAsync(obj, userID);
            var response = new Response<ReviewerFormsDto>(result);
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
            Trace.WriteLine(message, "PerformanceEmployeeReviewerController: Edit");
            return BadRequest(new { message });
        }
    }
}
