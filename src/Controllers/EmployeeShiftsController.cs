using System.Data;
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
//[Authorize]
public class EmployeeShiftsController : ControllerBase
{
    private readonly IServiceEmployeeShiftAsync<EmployeeShifts> _service;

    public EmployeeShiftsController(IServiceEmployeeShiftAsync<EmployeeShifts> service)
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

            var response = new Response<EmployeeShifts>(result);
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
            Trace.WriteLine(message, "EmployeeShiftsController");
            return BadRequest(new { message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmployeeShifts obj)
    {
        try
        {
            obj.UserIn = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.CreateAsync(obj);
            var response = new Response<EmployeeShifts>(result);
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
            Trace.WriteLine(message, "EmployeeShiftsController");
            return BadRequest(new { message });
        }

    }

    [HttpPut]
    public async Task<IActionResult> Edit([FromBody] EmployeeShifts obj)
    {
        try
        {
            obj.UserUp = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.EditAsync(obj);
            var response = new Response<EmployeeShifts>(result);
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
            Trace.WriteLine(message, "EmployeeShiftsController");
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
            Trace.WriteLine(message, "EmployeeShiftsController");
            return BadRequest(new { message });
        }
    }
    [HttpGet("template")]
    public async Task<IActionResult> GetTemplate(string filter = "")
    {
        try
        {
            var result = await _service.GetTemplateAsync(filter);
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
    [Route("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // Validate file type (Excel)
            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest("Only Excel files are allowed.");
            }

            // Save the file to a folder
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
            var filePath = Path.Combine(folderPath, file.FileName);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            DataTable dt = new DataTable();

            if (fileExtension == ".xlsx" || fileExtension == ".xls")
            {
                // Parse Excel file using EPPlus
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new OfficeOpenXml.ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                        return BadRequest("Invalid Excel file.");

                    // Get column headers dynamically
                    int totalColumns = worksheet.Dimension.End.Column;
                    int totalRows = worksheet.Dimension.End.Row;

                    for (int col = 1; col <= totalColumns; col++)
                    {
                        dt.Columns.Add(worksheet.Cells[1, col].Text.Trim());
                    }

                    // Read data into DataTable
                    for (int row = 2; row <= totalRows; row++)
                    {
                        DataRow dr = dt.NewRow();
                        for (int col = 1; col <= totalColumns; col++)
                        {
                            dr[col - 1] = worksheet.Cells[row, col].Text.Trim();
                        }
                        dt.Rows.Add(dr);
                    }
                }
            }
            bool isEmployeeBased = dt.Columns[1].ColumnName.Equals("nik", StringComparison.OrdinalIgnoreCase);
            bool isGroupBased = dt.Columns[1].ColumnName.Equals("group shift name", StringComparison.OrdinalIgnoreCase);

            if (!isEmployeeBased && !isGroupBased)
                return BadRequest("Invalid Excel template format.");

            var UserID = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.SetEmployeeShiftsAsync(dt, isEmployeeBased, UserID);

            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

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
