
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "ApplicantSocialMedia")]
    public class ApplicantSocialMedia : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SocialMediaID { get; set; }

        public long ApplicantID { get; set; }

        public string? LinkedInURL { get; set; }

        public string? GitHubURL { get; set; }

        public string? X_URL { get; set; }

        public string? InstagramURL { get; set; }

        public string? FacebookURL { get; set; }

        public string? PortfolioURL { get; set; }

        public string? OtherProfileURL { get; set; }
    }
}
