namespace AIApp10CodeReview.Models
{
    // One reviewable file from the pull request
    public class FileDiff
    {
        public string Path { get; set; } = string.Empty;

        // The patch text with the real new-file line number printed next to each line
        public string AnnotatedPatch { get; set; } = string.Empty;

        public int AddedLines { get; set; }

        // The new-file line numbers that this PR actually added (the only lines we may comment on)
        public HashSet<int> AddedLineNumbers { get; set; } = new();
    }
}
