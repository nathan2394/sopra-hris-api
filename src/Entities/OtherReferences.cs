
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "OtherReferences")]
    public class OtherReferences : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long OtherReferenceID { get; set; }

        public long ApplicantID { get; set; }

        public string ReferenceFullName { get; set; }

        public string ReferencePosition { get; set; }

        public string Relationship { get; set; }

        public string ReferenceCompanyName { get; set; }

        public string ReferencePhoneNumber { get; set; }

        public string ReferenceAddress { get; set; }

    }
}
