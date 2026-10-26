namespace AIApp10CodeReview.Models
{
    // What the model must return for one chunk: an object holding a list of findings
    public class ReviewResponse
    {
        public List<ReviewFinding> Findings { get; set; } = new();
    }
}
