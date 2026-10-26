# App 10 - AI Code Review Bot

Paste a public GitHub pull request URL. The bot reads the diff, reports likely bugs,
security issues, performance problems and missing tests, and a second, stronger model
double-checks every finding before you see it.

**Live demo:** https://aiapp10-codereview-hp-bkfnh8ghgthgezha.centralindia-01.azurewebsites.net/

Part of my public 12-month ".NET + AI" learning journey.
Portfolio: https://himanshu-ai-explains.github.io

---

## How it works

```
PR URL -> GitHub diff -> parse + number lines -> chunk -> REVIEW (gpt-4o-mini)
       -> deterministic FILTER (code) -> VERIFY (gpt-4o judge, 3 votes) -> findings
```

1. **Fetch** the diff from the GitHub REST API (`Accept: application/vnd.github.diff`).
2. **Parse** it into files and compute the real line numbers myself. Models cannot count
   lines reliably, so the model is shown numbered lines and never has to guess.
3. **Skip noise:** build folders, lock files, generated code, binaries, deleted files.
4. **Chunk** large diffs (about 4 characters per token) so each model call stays small.
5. **Review** each chunk with a cheap model that must answer in structured JSON.
6. **Filter in plain C#:** keep only findings on lines the PR actually added, drop
   duplicates, order by severity, cap the count. The model's output is treated as
   untrusted input.
7. **Verify** each surviving finding with a stronger model acting as a skeptical judge.
   The judge is asked three times and the majority wins, because a single verdict flips
   on borderline cases.

## Design decisions

| Decision | Why |
|---|---|
| Plain `HttpClient` + `IHttpClientFactory`, no GitHub SDK | One endpoint is needed; fewer dependencies, and it shows how the API works |
| Compute line numbers in code | A model that counts lines will be wrong; a wrong line breaks comment placement |
| Only `+` lines can carry a finding | You can only comment on code the PR changed |
| Structured output (`ResponseFormat = typeof(T)`) | Guaranteed parseable JSON; a parse failure degrades to "no findings", never a crash |
| Generate, then verify (LLM-as-judge) | The first pass is noisy ("add a try-catch", "could be null"); the judge removes speculation |
| Stronger model only for the judge | Reviewer runs per chunk (cheap model); judge runs per finding (small volume) |
| Majority of 3 judge votes | Measured: single verdicts flipped between runs on the same finding |
| Diff is DATA, never instructions | Prompt-injection defence; the bot can only comment, never approve or merge |

## API

`POST /api/review`

```json
{ "pullRequestUrl": "https://github.com/owner/repo/pull/3" }
```

Response (shortened):

```json
{
  "pullRequestUrl": "https://github.com/owner/repo/pull/3",
  "filesReviewed": 1,
  "chunksReviewed": 1,
  "findings": [
    {
      "file": "ReviewBotSamples/OrderService.cs",
      "line": 22,
      "severity": "Critical",
      "category": "Security",
      "message": "SQL injection due to concatenating user input into the query.",
      "suggestion": "Use parameterized queries."
    }
  ],
  "candidatesRejected": 0,
  "summary": "6 issue(s) found."
}
```

| Status | Meaning |
|---|---|
| 400 | Not a valid `https://github.com/owner/repo/pull/N` URL (anchored pattern, no look-alike hosts) |
| 404 | Pull request not found, or not accessible |
| 413 | Needs more than 6 review passes (cost cap) |
| 422 | GitHub refuses to return a diff this large (about 20,000 lines) |
| 429 | Demo rate limit reached |
| 502 | GitHub could not be reached |

## Run it locally

You need a .NET SDK, an OpenAI API key, and a GitHub fine-grained token with
**Public repositories (read-only)** access.

```
cd AIApp10CodeReview
dotnet user-secrets init
dotnet user-secrets set "OpenAIKey" "<your OpenAI key>"
dotnet user-secrets set "GitHubToken" "<your read-only GitHub token>"
dotnet run
```

Open the HTTPS address printed in the console. Secrets live in user-secrets locally and in
App Service environment variables when deployed. They are never in the repository.

## Security and cost controls

- Read-only GitHub token: a leaked token cannot change anything.
- Anchored URL validation against SSRF and look-alike hosts.
- Review cap of 6 chunks per request, reviewed one at a time.
- Rate limit: 3 reviews per hour per IP and 30 per day overall (worst-case spend is a
  known, small number). The counters are in memory, so they reset on restart and are
  per instance. A multi-instance deployment would need Redis or a gateway.
- Model output is rendered with `textContent`, never `innerHTML`, so a malicious PR
  cannot inject script through the findings.
- A hard monthly budget limit is set on the OpenAI project as the last line of defence.

## How well does it work? (measured, with limits)

- **PR #3** (planted flaws, tuned against): 6 of 6 found. Treat this as a smoke test,
  not proof, because the prompts were tuned on it.
- **PR #4** (held-out: 6 different planted flaws plus 5 clean code samples, run 3 times):
  found 5, 5 and 4 of 6. Every reported finding was a real flaw and nothing was flagged
  on the clean code.
- **PR #1** (a real contributor PR with no known bugs): 0, 1 and 0 findings across 3 runs.
  The one finding was a borderline comment about reading a key from console input.

This is a small sample (two PRs, 3 runs each, flaws I planted myself). It is a sanity
check, not a benchmark.

**Known limitations**

1. Concurrency bugs (for example a shared static `Dictionary` with no lock) were missed
   in every run.
2. Results vary between runs. A logged API key was found in 2 of 3 runs.
3. Severity labels are not stable. The same flaw can be Critical in one run and Warning
   in the next.
4. A finding can occasionally anchor on the method signature instead of the exact line.

The test PRs (#3 and #4) are public and marked "do not merge".

## Roadmap

- Post findings as PR review comments, via a GitHub Action.
- A dedicated concurrency review pass.
- Stable severity rules and a larger evaluation set.