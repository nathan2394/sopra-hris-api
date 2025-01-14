using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.src.Entities
{
    public class SalaryTemplateDTO
    {
        public long EmployeeID { get; set; }
        public string Nik { get; set; }
        public string Name { get; set; }
        public int? HKS { get; set; }
        public int? HKA { get; set; }
        public int? ATT { get; set; }
        public int? Late { get; set; }
        public int? OVT { get; set; }
        public decimal? OtherAllowances { get; set; }
        public decimal? OtherDeductions { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public SalaryTemplateDTO()
        {
            Month = DateTime.Now.Month;
            Year = DateTime.Now.Year;
        }
    }

    public class SalaryResultPayrollDTO
    {
        public long SalaryID { get; set; }
        public long EmployeeID { get; set; }
        public string Nik { get; set; }
        public string Name { get; set; }
        public long EmployeeTypeID { get; set; }
        public string EmployeeTypeName { get; set; }
        public long GroupID { get; set; }
        public string GroupName { get; set; }
        public long FunctionID { get; set; }
        public string FunctionName { get; set; }
        public long DivisionID { get; set; }
        public string DivisionName { get; set; }
        public long Month { get; set; }
        public long Year { get; set; }
        public long? HKS { get; set; }
        public long? HKA { get; set; }
        public long? ATT { get; set; }
        public long? OVT { get; set; }
        public long? Late { get; set; }
        public decimal? OtherAllowances { get; set; }
        public decimal? OtherDeductions { get; set; }
        public decimal? THP { get; set; }
        public string PayrollType { get; set; }
    }
    public class SalaryResultBankDTO
    {
        public long SalaryID { get; set; }
        public long EmployeeID { get; set; }
        public string Nik { get; set; }
        public string Name { get; set; }
        public string AccountNo { get; set; }
        public string Bank { get; set; }
        public decimal? THP { get; set; }
    }
}
