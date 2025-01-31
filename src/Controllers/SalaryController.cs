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

            // Save the file to a folder
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
            var filePath = Path.Combine(folderPath, file.FileName);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

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
                    var colCount = worksheet.Dimension.Columns;
                    var headerRow = worksheet.Cells[1, 1, 1, colCount];
                    var headerDict = new Dictionary<string, int>();

                    // Map header names to their respective column indexes
                    for (int col = 1; col <= colCount; col++)
                    {
                        string headerText = headerRow[1, col].Text.Trim().ToLower();
                        headerDict[headerText] = col;
                    }

                    for (int row = 2; row <= rowCount; row++) // Starting from row 2 to skip header
                    {
                        if (!string.IsNullOrEmpty(worksheet.Cells[row, headerDict["nik"]].Text))
                        {
                            var template = new SalaryTemplateDTO
                            {
                                EmployeeID = Convert.ToInt64(worksheet.Cells[row, headerDict["employeeid"]].Text),
                                Nik = worksheet.Cells[row, headerDict["nik"]].Text,
                                Name = worksheet.Cells[row, headerDict["name"]].Text,
                                HKS = Utility.TryParseNullableInt(worksheet.Cells[row, headerDict["hks"]].Text),
                                HKA =  Utility.TryParseNullableInt(worksheet.Cells[row, headerDict["hka"]].Text),
                                ATT = Utility.TryParseNullableInt(worksheet.Cells[row, headerDict["att"]].Text),
                                Late = Utility.TryParseNullableInt(worksheet.Cells[row, headerDict["late"]].Text),
                                OVT = Utility.TryParseNullableDecimal(worksheet.Cells[row, headerDict["ovt"]].Text),
                                OtherAllowances = Utility.TryParseNullableDecimal(worksheet.Cells[row, headerDict["otherallowances"]].Text),
                                OtherDeductions = Utility.TryParseNullableDecimal(worksheet.Cells[row, headerDict["otherdeductions"]].Text),
                                Month = Convert.ToInt32(worksheet.Cells[row, headerDict["month"]].Text),
                                Year = Convert.ToInt32(worksheet.Cells[row, headerDict["year"]].Text),
                                MEAL = Utility.TryParseNullableInt(worksheet.Cells[row, headerDict["meal"]].Text),
                                ABSENT = Utility.TryParseNullableInt(worksheet.Cells[row, headerDict["absent"]].Text),
                            };
                            salaryTemplates.Add(template);
                        }
                    }
                }
            }

            var validationErrors = ValidateSalaryTemplates(salaryTemplates);
            if (validationErrors.Any())
                return BadRequest(new { message = "Data validation failed.", errors = validationErrors });

            var UserID = Convert.ToInt64(User.FindFirstValue("id"));

            var result = await _service.GetSalaryResultPayrollAsync(salaryTemplates, UserID);
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
            Trace.WriteLine(message, "SalaryController");
            return BadRequest(new { message });
        }
    }
    private List<string> ValidateSalaryTemplates(List<SalaryTemplateDTO> templates)
    {
        var errors = new List<string>();
        var uniqueNiks = new HashSet<string>();

        foreach (var template in templates)
        {
            if (string.IsNullOrEmpty(template.Nik))
                errors.Add($"Missing Nik for EmployeeID: {template.EmployeeID}.");
            else if (!uniqueNiks.Add(template.Nik))
                errors.Add($"Duplicate Nik found: {template.Nik}.");

            if (string.IsNullOrEmpty(template.Name))
                errors.Add($"Missing Name for EmployeeID: {template.EmployeeID}.");            

            if (template.Month < 1 || template.Month > 12)
                errors.Add($"Invalid Month: {template.Month} for EmployeeID: {template.EmployeeID}. Must be between 1 and 12.");

            if (template.Year < 1900 || template.Year > DateTime.Now.Year)
                errors.Add($"Invalid Year: {template.Year} for EmployeeID: {template.EmployeeID}. Must be a valid year.");

            if (template.HKS < 0)
                errors.Add($"Invalid HKS: {template.HKS} for EmployeeID: {template.EmployeeID}. Must be non-negative.");

            if (template.OVT < 0)
                errors.Add($"Invalid OVT: {template.OVT} for EmployeeID: {template.EmployeeID}. Must be non-negative.");

        }

        return errors;
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
    [HttpGet("generatedata")]
    public async Task<IActionResult> GetGenerateData(string filter = "type:payroll", string date = "")
    {
        try
        {
            if (filter.Contains("bank"))
            {
                var result = await _service.GetGenerateBankAsync(filter, date);
                return Ok(result);
            }
            else if (filter.Contains("payroll"))
            {
                var result = await _service.GetGeneratePayrollResultAsync(filter, date);
                return Ok(result);
            }

            return BadRequest("Invalid Filter Type");
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
