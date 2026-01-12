namespace sopra_hris_api.Responses
{
    public class CandidateSummaryResponse
    {
        public string JobTitle { get; set; }
        public string Status { get; set; }
        public string Location { get; set; }
        public int KandidatCount { get; set; }
    }

    public class CandidateSummaryUserEmailResponse
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string JobTitle { get; set; }
    }

    public class CandidateSummaryEmailListResponse
    {
        public List<CandidateSummaryResponse> Summary { get; set; }
        public List<CandidateSummaryUserEmailResponse> UserEmails { get; set; }

        public CandidateSummaryEmailListResponse()
        {
            Summary = new List<CandidateSummaryResponse>();
            UserEmails = new List<CandidateSummaryUserEmailResponse>();
        }

        public CandidateSummaryEmailListResponse(List<CandidateSummaryResponse> summary, List<CandidateSummaryUserEmailResponse> userEmails)
        {
            Summary = summary;
            UserEmails = userEmails;
        }
    }
}