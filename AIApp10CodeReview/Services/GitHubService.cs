using System.Text.RegularExpressions;

namespace AIApp10CodeReview.Services
{
    // Identifies one pull request: owner, repo name, PR number
    public record PullRequestRef(string Owner, string Repo, int Number);
    public class GitHubService
    {
        private readonly HttpClient _http;

        public GitHubService(HttpClient http) => _http = http;

        // "https://github.com/owner/repo/pull/12" -> (owner, repo, 12)
        // The ^https://github.com/ anchor means only real GitHub URLs get through.
        // Later this endpoint will be public, so we never fetch an arbitrary URL a stranger sends us.

        public static PullRequestRef ParsePullRequestUrl(string url)
        {
            var match = Regex.Match(
                url ?? string.Empty,
                @"^https://github\.com/([\w.-]+)/([\w.-]+)/pull/(\d+)",
                RegexOptions.IgnoreCase);

            if (!match.Success)
                throw new ArgumentException("Not a valid GitHub pull request URL.");

            return new PullRequestRef(
                match.Groups[1].Value,
                match.Groups[2].Value,
                int.Parse(match.Groups[3].Value));
        }

        // Returns the whole PR as a unified diff (the +/- text git shows)
        public async Task<string> GetDiffAsync(PullRequestRef pr)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get, $"repos/{pr.Owner}/{pr.Repo}/pulls/{pr.Number}");

            // Same URL, but this Accept header asks GitHub for the diff text instead of JSON
            request.Headers.Accept.ParseAdd("application/vnd.github.diff");

            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"GitHub returned {(int)response.StatusCode} for {pr.Owner}/{pr.Repo}#{pr.Number}.");

            return await response.Content.ReadAsStringAsync();
        }

    }
}
