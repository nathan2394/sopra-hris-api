
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "OrganizationalHistory")]
    public class OrganizationalHistory : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long OrganizationHistoryID { get; set; }

        public long ApplicantID { get; set; }

        public string OrganizationName { get; set; }

        public string ActivityType { get; set; }

        public string Position { get; set; }

        public int? Year { get; set; }

        public string LocationCity { get; set; }

    }
}
