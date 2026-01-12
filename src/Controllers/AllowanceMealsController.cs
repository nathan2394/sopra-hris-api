using System.Data;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using sopra_hris_api.Entities;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Services;

namespace sopra_hris_api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class AllowanceMealsController : ControllerBase
{
    private readonly IServiceUploadAsync<AllowanceMeals> _service;

    public AllowanceMealsController(IServiceUploadAsync<AllowanceMeals> service)
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
    [HttpGet]
    [Route("template")]
    public async Task<IActionResult> GetTemplate(string search = "", string sort = "", string filter = "", string date = "")
    {
        try
        {
            var result = await _service.GetTemplate(search, sort, filter, date);
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

            var response = new Response<AllowanceMeals>(result);
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
            Trace.WriteLine(message, "AllowanceMealsController");
            return BadRequest(new { message });
        }
    }
    [HttpPost]
    [Route("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
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
                return BadRequest("Only Excel files are allowed.");

            // Save the file to a folder
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
            var filePath = Path.Combine(folderPath, file.FileName);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }// Parse Excel file using EPPlus
            DataTable dt = new DataTable();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new OfficeOpenXml.ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0]; // Assume the first worksheet contains the data
                if (worksheet == null)
                    return BadRequest("Invalid Excel file.");

                int totalColumns = worksheet.Dimension.End.Column;
                int totalRows = worksheet.Dimension.End.Row;

                for (int col = 1; col <= totalColumns; col++)
                {
                    dt.Columns.Add(worksheet.Cells[1, col].Text.Trim());
                }
                for (int row = 2; row <= totalRows; row++)
                {
                    DataRow dr = dt.NewRow();
                    bool isRowValid = true;
                    for (int col = 1; col <= totalColumns; col++)
                    {

                        string columnName = dt.Columns[col - 1].ColumnName;

                        string cellValue = worksheet.Cells[row, col].Text.Trim();
                        if (string.IsNullOrEmpty(cellValue) && columnName == "employeeID")
                        {
                            isRowValid = false;
                            break;
                        }
                        // Handle "meal" column
                        if (columnName.Equals("meal", StringComparison.OrdinalIgnoreCase))
                        {
                            int mealValue = 0;
                            int.TryParse(cellValue, out mealValue);
                            dr[col - 1] = mealValue;
                        }
                        else
                        {
                            dr[col - 1] = cellValue;
                        }
                    }

                    if (isRowValid)
                        dt.Rows.Add(dr);
                }
            }
            var UserID = Convert.ToInt64(User.FindFirstValue("id"));
            var result = await _service.UploadAsync(dt, UserID);

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
            Trace.WriteLine(message, "AllowanceMealsController");
            return BadRequest(new { message });
        }

    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AllowanceMeals obj)
    {
        try
        {
            obj.UserIn = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.CreateAsync(obj);
            var response = new Response<AllowanceMeals>(result);
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
            Trace.WriteLine(message, "AllowanceMealsController");
            return BadRequest(new { message });
        }

    }

    [HttpPut]
    public async Task<IActionResult> Edit([FromBody] AllowanceMeals obj)
    {
        try
        {
            obj.UserUp = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.EditAsync(obj);
            var response = new Response<AllowanceMeals>(result);
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
            Trace.WriteLine(message, "AllowanceMealsController");
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
            Trace.WriteLine(message, "AllowanceMealsController");
            return BadRequest(new { message });
        }
    }
}
