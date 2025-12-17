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
public class TestSessionsController : ControllerBase
{
    private readonly IServiceAsync<TestSessions> _service;
    private readonly TestSessionService _testSessionService;

    public TestSessionsController(IServiceAsync<TestSessions> service, TestSessionService testSessionService)
    {
        _service = service;
        _testSessionService = testSessionService;
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

            var response = new Response<TestSessions>(result);
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
            Trace.WriteLine(message, "TestSessionsController");
            return BadRequest(new { message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TestSessions obj)
    {
        try
        {
            obj.UserIn = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.CreateAsync(obj);
            
            // Get questions and answers for the created session
            var sessionWithQuestions = await _testSessionService.GetQuestionsForSessionAsync(result.SessionID);
            
            var response = new Response<TestSessionWithQuestionsResponse>(sessionWithQuestions);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            Trace.WriteLine(ex.Message, "TestSessionsController");
            return BadRequest(new { message = ex.Message });
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
            Trace.WriteLine(message, "TestSessionsController");
            return BadRequest(new { message });
        }

    }

    [HttpPut]
    public async Task<IActionResult> Edit([FromBody] TestSessions obj)
    {
        try
        {
            obj.UserUp = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.EditAsync(obj);
            var response = new Response<TestSessions>(result);
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
            Trace.WriteLine(message, "TestSessionsController");
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
            Trace.WriteLine(message, "TestSessionsController");
            return BadRequest(new { message });
        }
    }
    [HttpGet("{id}/Details")]
    public async Task<IActionResult> GetSessionDetails(long id)
    {
        try
        {
            var result = await _testSessionService.GetSessionDetailsAsync(id);
            if (result == null || result.Count == 0)
                return BadRequest(new { message = "No test responses found for this session." });

            var response = new ListResponse<TestSessionDetailDTO>(result, result.Count, 0);
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
            Trace.WriteLine(message, "TestSessionsController");
            return BadRequest(new { message });
        }
    }

    [HttpGet("{id}/ScoreByCategory")]
    public async Task<IActionResult> GetSessionScoreByCategory(long id)
    {
        try
        {
            var result = await _testSessionService.GetSessionScoreByCategoryAsync(id);
            if (result == null || result.Count == 0)
                return BadRequest(new { message = "No test responses found for this session." });

            var response = new ListResponse<TestSessionScoreByCategoryDTO>(result, result.Count, 0);
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
            Trace.WriteLine(message, "TestSessionsController");
            return BadRequest(new { message });
        }
    }

    [HttpGet("{id}/OverallScore")]
    public async Task<IActionResult> GetSessionOverallScore(long id)
    {
        try
        {
            var result = await _testSessionService.GetSessionOverallScoreAsync(id);
            if (result == null)
                return BadRequest(new { message = "No test responses found for this session." });

            var response = new Response<TestSessionOverallScoreDTO>(result);
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
            Trace.WriteLine(message, "TestSessionsController");
            return BadRequest(new { message });
        }
    }
}
