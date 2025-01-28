using System.Collections.Generic;
using System.IO;
using sopra_hris_api.src.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace sopra_hris_api.Responses
{
	public class ListResponse<T>
	{
		public IEnumerable<T> Data { get; set; }
		public int Total { get; set; }
		public int Page { get; set; }

		public ListResponse(IEnumerable<T> data, int total, int page)
		{
			Data = data;
			Total = total;
			Page = page;
		}
	}
    public class ListResponseTemplate<T>
    {
        public IEnumerable<T> Data { get; set; }

        public ListResponseTemplate(IEnumerable<T> data)
        {
            Data = data;
        }
    }

    public class ListResponseUploadTemplate<T>
    {
        public IEnumerable<T> Data { get; set; }
        public IEnumerable<SalaryPayrollSummaryDTO> DataSummary { get; set; }
        public IEnumerable<SalaryPayrollSummaryTotalDTO> DataSummaryTotal { get; set; }

        public ListResponseUploadTemplate(IEnumerable<T> data, IEnumerable<SalaryPayrollSummaryDTO> dataSummary, IEnumerable<SalaryPayrollSummaryTotalDTO> dataSummaryTotal)
        {
            Data = data;
            DataSummary = dataSummary;
            DataSummaryTotal = dataSummaryTotal;
        }
    }

    public class ListResponseFilter<T>
	{
		public IEnumerable<FilterGroup> Filters { get; set; }

		public ListResponseFilter(IEnumerable<FilterGroup> filters)
		{
			Filters = filters;
		}


	}
	public class FilterInfo
	{
		public long? ID { get; set; }
		public string? Name { get; set; }
		public decimal? Value { get; set; }
		public decimal? MinValue { get; set; }
		public decimal? MaxValue { get; set; }
		public long? Count { get; set; }

	}

	public class FilterGroup
	{
		public string GroupName { get; set; }
		public List<FilterInfo> Filter { get; set; }
	}
}
