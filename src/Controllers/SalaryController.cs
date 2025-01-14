using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using sopra_hris_api.Entities;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Entities;
using sopra_hris_api.src.Services;

namespace sopra_hris_api.Controllers;

[ApiController]
[Route("[controller]")]
public class SalaryController : ControllerBase
{
    private readonly IServiceSalaryAsync<Salary> _service;

    public SalaryController(IServiceSalaryAsync<Salary> service)
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

            var response = new Response<Salary>(result);
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
            Trace.WriteLine(message, "SalaryController");
            return BadRequest(new { message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Salary obj)
    {
        try
        {
            obj.UserIn = Convert.ToInt64(1);

            var result = await _service.CreateAsync(obj);
            var response = new Response<Salary>(result);
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
            Trace.WriteLine(message, "SalaryController");
            return BadRequest(new { message });
        }

    }

    [HttpPut]
    public async Task<IActionResult> Edit([FromBody] Salary obj)
    {
        try
        {
            obj.UserUp = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.EditAsync(obj);
            var response = new Response<Salary>(result);
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
            Trace.WriteLine(message, "SalaryController");
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
            Trace.WriteLine(message, "SalaryController");
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

            // Validate file type (CSV or Excel)
            var allowedExtensions = new[] { ".csv", ".xlsx", ".xls" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest("Only CSV or Excel files are allowed.");
            }

            // Example: Save the file to a folder
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles", file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            List<SalaryTemplateDTO> salaryTemplates = new List<SalaryTemplateDTO>();

            if (fileExtension == ".csv")
            {
                // Parse CSV file using CsvHelper
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvHelper.CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    salaryTemplates = csv.GetRecords<SalaryTemplateDTO>().ToList();
                }
            }
            else if (fileExtension == ".xlsx" || fileExtension == ".xls")
            {
                // Parse Excel file using EPPlus
                salaryTemplates = new List<SalaryTemplateDTO>();
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new OfficeOpenXml.ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0]; // Assume the first worksheet contains the data
                    var rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++) // Starting from row 2 to skip header
                    {
                        if (!string.IsNullOrEmpty(worksheet.Cells[row, 2].Text))
                        {
                            var template = new SalaryTemplateDTO
                            {
                                EmployeeID = Convert.ToInt64(worksheet.Cells[row, 1].Text),
                                Nik = worksheet.Cells[row, 2].Text,
                                Name = worksheet.Cells[row, 3].Text,
                                HKS = string.IsNullOrEmpty(worksheet.Cells[row, 4].Text) ? (int?)null : Convert.ToInt32(worksheet.Cells[row, 4].Text),
                                HKA = string.IsNullOrEmpty(worksheet.Cells[row, 5].Text) ? (int?)null : Convert.ToInt32(worksheet.Cells[row, 5].Text),
                                ATT = string.IsNullOrEmpty(worksheet.Cells[row, 6].Text) ? (int?)null : Convert.ToInt32(worksheet.Cells[row, 6].Text),
                                Late = string.IsNullOrEmpty(worksheet.Cells[row, 7].Text) ? (int?)null : Convert.ToInt32(worksheet.Cells[row, 7].Text),
                                OVT = string.IsNullOrEmpty(worksheet.Cells[row, 8].Text) ? (int?)null : Convert.ToInt32(worksheet.Cells[row, 8].Text),
                                OtherAllowances = string.IsNullOrEmpty(worksheet.Cells[row, 9].Text) ? (decimal?)null : Convert.ToDecimal(worksheet.Cells[row, 9].Text),
                                OtherDeductions = string.IsNullOrEmpty(worksheet.Cells[row, 10].Text) ? (decimal?)null : Convert.ToDecimal(worksheet.Cells[row, 10].Text),
                                Month = Convert.ToInt32(worksheet.Cells[row, 11].Text),
                                Year = Convert.ToInt32(worksheet.Cells[row, 12].Text)
                            };
                            salaryTemplates.Add(template);
                        }
                    }
                }
            }

            // Pass the parsed List<SalaryTemplateDTO> to the service method
            var result = await _service.GetSalaryResultPayrollAsync(salaryTemplates);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
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
            Trace.WriteLine(message, "SalaryController");
            return BadRequest(new { message });
        }
    }
    [HttpGet("template")]
    public async Task<IActionResult> GetSalaryTemplate(string search = "", string sort = "", string filter = "")
    {
        try
        {
            var result = await _service.GetSalaryTemplateAsync(search, sort, filter);

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
            Trace.WriteLine(message, "SalaryController");
            return BadRequest(new { message });
        }
    }
}
