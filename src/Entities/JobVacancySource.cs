
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "JobVacancySource")]
    public class JobVacancySource : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long JobVacancySourceId { get; set; }
        public string SourceCode { get; set; }
        public string SourceName { get; set; }

    }
}
