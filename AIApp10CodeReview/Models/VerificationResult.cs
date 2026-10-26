namespace AIApp10CodeReview.Models
{
    // The judge's verdict on one proposed finding
    public class VerificationResult
    {
        public bool IsRealProblem { get; set; }

        // Why the judge decided that, in one sentence (also useful for debugging the bot)
        public string Reason { get; set; } = string.Empty;
    }
}
