namespace AIApp10CodeReview.Models
{
    public class ReviewResult
    {
        public string PullRequestUrl { get; set; } = "";
        public int FilesReviewed { get; set; }
        public int ChunksReviewed { get; set; }

        // Findings the judge confirmed as real problems
        public List<ReviewFinding> Findings { get; set; } = new();

        // How many candidates the bot considered and then dropped, shown for transparency
        public int CandidatesRejected { get; set; }

        // A plain-English summary for humans, e.g. "2 issues found" or "No issues found"
        public string Summary { get; set; } = "";
    }
}
