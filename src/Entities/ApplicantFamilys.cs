
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "ApplicantFamilys")]
    public class ApplicantFamilys : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long FamilyID { get; set; }

        public long ApplicantID { get; set; }

        public string RelationshipType { get; set; }

        public string FamilyName { get; set; }

        public string Gender { get; set; }

        public string FamilyPlaceOfBirth { get; set; }

        public string FamilyDateOfBirth { get; set; }

        public string ParentEducation { get; set; }

        public string FamilyOccupation { get; set; }

    }
}
