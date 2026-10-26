using AIApp10CodeReview.Models;

namespace AIApp10CodeReview.Services;

public class FindingFilter
{
    public const int MaxFindings = 8;

    public List<ReviewFinding> Apply(List<ReviewFinding> findings, List<FileDiff> files)
    {
        // For each file: the set of line numbers this PR actually added
        var addedLines = files.ToDictionary(
            f => f.Path, f => f.AddedLineNumbers, StringComparer.OrdinalIgnoreCase);

        return findings
            // Rule 1: the finding must point at a line this PR added. Anything else is rejected.
            .Where(f => addedLines.TryGetValue(f.File, out var lines) && lines.Contains(f.Line))
            // Rule 2: no empty comments
            .Where(f => !string.IsNullOrWhiteSpace(f.Message))
            // Rule 3: the same problem reported again is dropped; keep the first
            .GroupBy(f => (f.Category, Text: f.Message.Trim().ToLowerInvariant()))
            .Select(g => g.First())
            // Rule 4: worst first, then cap the total per pull request
            .OrderByDescending(f => f.Severity)
            .ThenBy(f => f.File)
            .ThenBy(f => f.Line)
            .Take(MaxFindings)
            .ToList();
    }
}