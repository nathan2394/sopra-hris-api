﻿using System.Data;
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
    public interface IServiceEmployeeAsync<T>
    {
        Task<ListResponse<T>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date, long UserID, long EmployeeID, long GroupID);
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
        Task<ListResponseTemplate<T>> SetEmployeeShiftsAsync(DataTable templates, bool isEmployeeBased, long UserID);

    }
}
