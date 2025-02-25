using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;

namespace sopra_hris_api.src.Entities
{
    public class EmployeeGroupShiftTemplate
    {
        public long? EmployeeID { get; set; }
        public string? Nik { get; set; }
        public string? Name { get; set; }
        public long? GroupShiftID { get; set; }
        public string? GroupShiftCode { get; set; }
        public string? GroupShiftName { get; set; }
    }
}
