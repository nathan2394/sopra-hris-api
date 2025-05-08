
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "FAQ")]
    public class FAQ : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long FAQID { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
    }
}
