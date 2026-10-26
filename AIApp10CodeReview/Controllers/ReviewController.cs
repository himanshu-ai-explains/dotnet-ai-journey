using AIApp10CodeReview.Models;
using AIApp10CodeReview.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AIApp10CodeReview.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    // Cost cap: each chunk costs one model call, so refuse anything bigger up front.
    private const int MaxChunks = 6;

    private readonly GitHubService _github;
    private readonly DiffChunker _chunker;
    private readonly ReviewService _reviewer;
    private readonly FindingFilter _filter;
    private readonly FindingVerifier _verifier;
    private readonly ILogger<ReviewController> _logger;

    public ReviewController(
        GitHubService github,
        DiffChunker chunker,
        ReviewService reviewer,
        FindingFilter filter,
        FindingVerifier verifier,
        ILogger<ReviewController> logger)
    {
        _github = github;
        _chunker = chunker;
        _reviewer = reviewer;
        _filter = filter;
        _verifier = verifier;
        _logger = logger;
    }

    [EnableRateLimiting("review")]
    [HttpPost]
    public async Task<ActionResult<ReviewResult>> Review([FromBody] ReviewRequest request)
    {
        // 1. Validate the URL. The parser only accepts https://github.com/owner/repo/pull/N
        PullRequestRef pr;
        try
        {
            pr = GitHubService.ParsePullRequestUrl(request.PullRequestUrl);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Rejected pull request URL");
            return Problem(statusCode: 400, title: "Invalid pull request URL",
                detail: "Use a URL like https://github.com/owner/repo/pull/3");
        }

        // 2. Fetch the diff. Map GitHub failures to clear, safe messages.
        string diff;
        try
        {
            diff = await _github.GetDiffAsync(pr);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "GitHub diff fetch failed for {Url}", request.PullRequestUrl);

            return GetStatus(ex) switch
            {
                406 => Problem(statusCode: 422, title: "Pull request too large",
                    detail: "GitHub will not return a diff this big. Try a smaller pull request."),
                404 => Problem(statusCode: 404, title: "Pull request not found",
                    detail: "It does not exist, or the bot has no access to that repository."),
                _ => Problem(statusCode: 502, title: "GitHub request failed",
                    detail: "Could not read the pull request from GitHub. Try again later.")
            };
        }

        // 3. Parse and chunk. Cap the size BEFORE spending any model money.
        var files = _chunker.Parse(diff);
        if (files.Count == 0)
        {
            return Ok(new ReviewResult
            {
                PullRequestUrl = request.PullRequestUrl,
                Summary = "No reviewable code in this pull request."
            });
        }

        var chunks = _chunker.Chunk(files);
        if (chunks.Count > MaxChunks)
        {
            return Problem(statusCode: 413, title: "Pull request too large to review",
                detail: $"This pull request needs {chunks.Count} review passes. The limit is {MaxChunks}.");
        }

        // 4. Review chunks ONE AT A TIME so a single request never fans out model calls.
        var raw = new List<ReviewFinding>();
        foreach (var chunk in chunks)
            raw.AddRange(await _reviewer.ReviewChunkAsync(chunk));

        // 5. Deterministic filter, then the stronger judge.
        var candidates = _filter.Apply(raw, files);

        var confirmed = new List<ReviewFinding>();
        foreach (var finding in candidates)
        {
            var file = files.FirstOrDefault(f =>
                string.Equals(f.Path, finding.File, StringComparison.OrdinalIgnoreCase));
            if (file is null) continue;

            var verdict = await _verifier.VerifyAsync(finding, file);
            if (verdict.IsRealProblem) confirmed.Add(finding);
        }

        return Ok(new ReviewResult
        {
            PullRequestUrl = request.PullRequestUrl,
            FilesReviewed = files.Count,
            ChunksReviewed = chunks.Count,
            Findings = confirmed,
            CandidatesRejected = candidates.Count - confirmed.Count,
            Summary = confirmed.Count == 0
                ? "No issues found."
                : $"{confirmed.Count} issue(s) found."
        });
    }

    private static int? GetStatus(HttpRequestException ex)
    {
        if (ex.StatusCode.HasValue) return (int)ex.StatusCode.Value;

        // Fallback: our own exception text contains the status code.
        foreach (var code in new[] { 401, 403, 404, 406 })
            if (ex.Message.Contains(code.ToString())) return code;

        return null;
    }
}