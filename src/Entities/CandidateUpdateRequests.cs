namespace sopra_hris_api.Entities
{
    public class UpdateFitScoreRequest
    {
        public decimal? FitScore { get; set; }
    }

    public class UpdateGradeLevelRequest
    {
        public string GradeLevel { get; set; }
    }

    public class UpdateFitScoreAndGradeLevelRequest
    {
        public decimal? FitScore { get; set; }
        public string GradeLevel { get; set; }
    }
}
