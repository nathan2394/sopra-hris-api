
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "LanguageSkills")]
    public class LanguageSkills : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long LanguageSkillID { get; set; }

        public long ApplicantID { get; set; }

        public string LanguageName { get; set; }

        public string SpeakingProficiency { get; set; }

        public string WritingProficiency { get; set; }

    }
}
