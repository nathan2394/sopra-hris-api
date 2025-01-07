using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using System.Collections.Generic;
using sopra_hris_api.Responses;
using sopra_hris_api.Entities;

namespace sopra_hris_api.Services
{
    public class CompanyService
    {
        public static async Task<ListResponse<Company>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                return new ListResponse<Company>(null, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
    }
}