
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sopra_hris_api.Entities
{
    [Table(name: "ApplicantOtherInfo")]
    public class ApplicantOtherInfo : Entity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long OtherInfoID { get; set; }

        public long ApplicantID { get; set; }

        public decimal? ExpectedSalary { get; set; }

        public string AvailabilityToStart { get; set; }

        public bool? AppliedBeforeAtSopra { get; set; }

        public string AppliedBeforeExplanation { get; set; }

        public bool? HasRelativeAtSopra { get; set; }

        public string RelativeAtSopraExplanation { get; set; }

        public bool? AgreesToContactReferences { get; set; }

        public bool? ReadyForShiftWork { get; set; }

        public bool? ReadyForOutOfTownAssignments { get; set; }

        public bool? ReadyForOutOfTownPlacement { get; set; }

        public bool? HasSeriousIllnessOrInjury { get; set; }

        public string SeriousIllnessOrInjuryExplanation { get; set; }

        public bool? HasPoliceRecord { get; set; }

        public string PoliceRecordExplanation { get; set; }

        public bool? HasPermanentPhysicalImpairment { get; set; }

        public string PhysicalImpairmentExplanation { get; set; }

    }
}
