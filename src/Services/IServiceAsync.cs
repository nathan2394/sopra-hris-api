using System.Data;
using sopra_hris_api.Entities;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Entities;

namespace sopra_hris_api.src.Services
{
    public interface IServiceAsync<T>
    {
        Task<ListResponse<T>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date);
        Task<T> GetByIdAsync(long id);
        Task<T> CreateAsync(T data);
        Task<T> EditAsync(T data);
        Task<bool> DeleteAsync(long id, long userID);
    }
    public interface IServiceDashboardAsync<T>
    {
        Task<ListResponseTemplate<DashboardDTO.DashboardAttendanceByShift>> GetAttendanceByShift(string filter, string date);
        Task<ListResponseTemplate<DashboardDTO.DashboardAttendanceNormalAbnormal>> GetAttendanceNormalAbnormal(string filter, string date);
        Task<ListResponseTemplate<DashboardDTO.DashboardAttendanceSummary>> GetAttendanceSummary(string filter, string date);
        Task<ListResponseTemplate<DashboardDTO.DashboardBudgetOvertimes>> GetBudgetOvertimes(string filter, string date);
        Task<ListResponseTemplate<DashboardDTO.DashboardApproval>> GetApproval(string filter, string date);
        Task<ListResponseTemplate<DashboardDTO.DashboardDetaillOVT>> GetDetailOVT(string filter, string date);
        Task<ListResponseTemplate<DashboardDTO.DashboardDetaillLate>> GetDetailLate(string filter, string date);
        Task<ListResponseTemplate<DashboardDTO.DashboardDetaillMeals>> GetDetailMeals(string filter, string date);
        Task<ListResponseTemplate<DashboardDTO.DashboardDetaillAbsent>> GetDetaillAbsent(string filter, string date);
    }
    public interface IServiceUploadAsync<T>
    {
        Task<ListResponse<T>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date);
        Task<T> GetByIdAsync(long id);
        Task<T> CreateAsync(T data);
        Task<T> EditAsync(T data);
        Task<ListResponseTemplate<T>> UploadAsync(DataTable data, long UserID);
        Task<ListResponseTemplate<AllowanceMealDTO>> GetTemplate(string search, string sort, string filter, string date);
        Task<bool> DeleteAsync(long id, long userID);
    }
    public interface IServiceOVTAsync<T>
    {
        Task<ListResponse<T>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date);
        Task<T> GetByIdAsync(long id);
        Task<T> CreateAsync(T data);
        Task<T> EditAsync(T data);
        Task<string> BulkCreateAsync(BulkOvertimes data);
        Task<bool> DeleteAsync(long id, long userID);
        Task<bool> ApprovalAsync(List<ApprovalDTO> data);
        Task<ListResponse<T>> GetAllApprovalAsync(int limit, int page, int total, string search, string sort, string filter, string date);
    }
    public interface IServiceEmployeeTransferShiftAsync<T>
    {
        Task<ListResponse<T>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date);
        Task<T> GetByIdAsync(long id);
        Task<T> CreateAsync(T data);
        Task<T> EditAsync(T data);
        Task<string> BulkCreateAsync(BulkEmployeeTransferShifts data);
        Task<bool> DeleteAsync(long id, long userID);
        Task<bool> ApprovalAsync(List<ApprovalDTO> data);
        Task<ListResponse<T>> GetAllApprovalAsync(int limit, int page, int total, string search, string sort, string filter, string date);
    }
    public interface IServiceUnattendanceOVTAsync<T>
    {
        Task<ListResponse<T>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date);
        Task<T> GetByIdAsync(long id);
        Task<T> CreateAsync(T data);
        Task<T> EditAsync(T data);
        Task<bool> DeleteAsync(long id, long userID);
        Task<bool> ApprovalAsync(List<ApprovalDTO> data);
        Task<ListResponse<T>> GetAllApprovalAsync(int limit, int page, int total, string search, string sort, string filter, string date);
    }
    public interface IServiceAttendancesAsync<T>
    {
        Task<ListResponseTemplate<AttendanceSummary>> GetAllAsync(string filter, string date);
        Task<ListResponseTemplate<AttendanceCheck>> GetListCheckAsync(string filter, string date);
        Task<ListResponse<T>> GetAllAsync(int limit, int page, int total, long id, string date);
        Task<ListResponseTemplate<AttendanceDetails>> GetDetailAsync(long id, string date);
        Task<ListResponseTemplate<AttendanceShift>> GetDetailShiftsAsync(long id, string date);
        Task<AttendanceDetails> SaveAttendancesAsync(AttendanceDTO attendance);
        Task<bool> DeleteAsync(long id, long userID);
        Task<T> CreateAsync(T data);
        Task<T> EditAsync(T data);
    }
    public interface IServiceEmployeeAsync<T>
    {
        Task<ListResponse<T>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date);
        Task<ListResponse<T>> GetList(int limit, int page, int total, string search, string sort,
        string filter, string date);
        Task<T> GetByIdAsync(long id);
        Task<T> CreateAsync(T data);
        Task<T> EditAsync(T data);
        Task<bool> DeleteAsync(long id, long userID);
    }
    public interface IServiceSalaryAsync<T>
    {
        Task<ListResponse<T>> GetAllAsync(int limit, int page, int total, string search, string sort,
       string filter, string date);
        Task<bool> SetConfirmation(List<SalaryConfirmation> salaries, long userID);
        Task<ListResponseTemplate<SalaryDetailReportsDTO>> GetEmployeeSalaryHistoryAsync(long EmployeeID, long Month, long Year, long UserID);
        Task<ListResponseTemplate<MasterEmployeePayroll>> GetMasterSalaryAsync(long EmployeeID);
        Task<ListResponseTemplate<SalaryTemplateDTO>> GetSalaryTemplateAsync(string search, string sort, string filter);
        Task<ListResponseUploadTemplate<SalaryDetailReportsDTO>> GetSalaryResultPayrollAsync(List<SalaryTemplateDTO> template, long userID);
        Task<ListResponseTemplate<SalaryPayrollBankDTO>> GetGenerateBankAsync(string filter);
        Task<ListResponseTemplate<SalaryDetailReportsDTO>> GetGeneratePayrollResultAsync(string filter, long UserID);
        Task<ListResponseTemplate<SalaryCalculatorModel>> SetCalculator(SalaryCalculatorTemplate request);
        Task<ListResponseUploadTemplate<SalaryDetailReportsDTO>> SetCalculateEmployeeSalary(CalculateEmployeeSalary request, long UserID);
    }
    public interface IServiceSalaryDetailsAsync<T>
    {
        Task<ListResponse<T>> GetAllAsync(int limit, int page, int total, string search, string sort,
       string filter, string date);
        Task<T> GetByIdAsync(long id);
        Task<ListResponseTemplate<SalaryDetailReportsDTO>> GetSalaryDetailReports(string filter);
        Task<SalaryDetailReportsDTO> GetSalaryDetails(long id);
    }
    public interface IServiceEmployeeShiftAsync<T>
    {
        Task<ListResponse<T>> GetAllAsync(int limit, int page, int total, string search, string sort,
         string filter, string date);
        Task<T> GetByIdAsync(long id);
        Task<T> CreateAsync(T data);
        Task<T> EditAsync(T data);
        Task<bool> DeleteAsync(long id, long userID);
        Task<ListResponseTemplateShift<EmployeeGroupShiftTemplate>> GetTemplateAsync(string filter);
        Task<ListResponseTemplate<EmployeeShiftsDTO>> SetEmployeeShiftsAsync(DataTable templates, bool isEmployeeBased, long UserID);
    }
}
