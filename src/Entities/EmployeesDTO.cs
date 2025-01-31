
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace sopra_hris_api.Entities
{
    public class EmployeesDTO
    {
        public Employees Employee { get; set; }
        public List<EmployeeDetails> Details { get; set; }
    }
}
