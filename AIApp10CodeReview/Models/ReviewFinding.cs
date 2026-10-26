using System.Text.Json.Serialization;

namespace AIApp10CodeReview.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Severity
{
    Info,
    Warning,
    Critical
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FindingCategory
{
    Bug,
    Security,
    MissingTest,
    Performance
}

// One piece of feedback the bot wants to leave on one line of the pull request.
public class ReviewFinding
{
    // Path of the file in the repo, e.g. "Services/ChatService.cs"
    public string File { get; set; } = string.Empty;

    // Line number in the NEW version of the file. GitHub needs this to attach an inline comment.
    public int Line { get; set; }

    public Severity Severity { get; set; }

    public FindingCategory Category { get; set; }

    // What is wrong and why it matters
    public string Message { get; set; } = string.Empty;

    // How to fix it. Optional, so it can be empty.
    public string Suggestion { get; set; } = string.Empty;
}