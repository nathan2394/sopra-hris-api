
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Jobs")]
    public class Jobs : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long JobID { get; set; }

        public long CompanyID { get; set; }

        public string JobTitle { get; set; }

        public string JobDescription { get; set; }

        public string JobType { get; set; }
        public string Department { get; set; }
        public string Location { get; set; }

        public decimal? SalaryMin { get; set; }

        public decimal? SalaryMax { get; set; }

        public string Tags { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? PublicationDate { get; set; }
        public DateTime? ExpirationDate { get; set; }

    }
}
