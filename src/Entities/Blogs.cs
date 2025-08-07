
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "Blogs")]
    public class Blogs : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long BlogID { get; set; }
        public string? BlogTitle_id { get; set; }
        public string? BlogTitle_en { get; set; }
        public string? BlogImage { get; set; }
        public string? BlogContent_id { get; set; }
        public string? BlogContent_en { get; set; }
        public string? BlogThumbnail { get; set; }
        public string? BlogVideo { get; set; }
        public string? BlogTags { get; set; }
    }
}
