using AIApp10CodeReview.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace AIApp10CodeReview.Services
{
    public class DiffChunker
    {
        // Folders and file types that are never worth a review (and cost tokens)
        private static readonly string[] SkipFolders =
            { "bin/", "obj/", ".vs/", "node_modules/", "packages/" };

        private static readonly string[] SkipSuffixes =
        {
        "package-lock.json", "yarn.lock", ".lock", ".min.js", ".min.css",
        ".designer.cs", ".g.cs", ".png", ".jpg", ".jpeg", ".gif", ".ico",
        ".pdf", ".zip", ".dll", ".exe", ".pdb", ".mp4", ".snk"
    };

        // "@@ -1,11 +1,32 @@": group 1 captures the starting line number in the NEW file
        private static readonly Regex HunkHeader =
            new(@"^@@ -\d+(?:,\d+)? \+(\d+)(?:,\d+)? @@", RegexOptions.Compiled);

        private static bool IsNoise(string path)
        {
            string p = "/" + path.ToLowerInvariant();
            return SkipFolders.Any(f => p.Contains("/" + f))
                || SkipSuffixes.Any(s => p.EndsWith(s));
        }

        // Raw unified diff -> one FileDiff per reviewable file, with line numbers added
        public List<FileDiff> Parse(string diff)
        {
            var files = new List<FileDiff>();
            string[] lines = diff.Replace("\r\n", "\n").Split('\n');

            string? path = null;
            var sb = new StringBuilder();
            var addedNumbers = new HashSet<int>();
            int added = 0;
            int newLine = 0;
            bool inHunk = false;

            void Finish()
            {
                if (path != null && added > 0 && !IsNoise(path))
                {
                    files.Add(new FileDiff
                    {
                        Path = path,
                        AnnotatedPatch = sb.ToString().TrimEnd(),
                        AddedLines = added,
                        AddedLineNumbers = addedNumbers
                    });
                }
                path = null;
                sb.Clear();
                addedNumbers = new HashSet<int>();   // a fresh set for the next file
                added = 0;
                inHunk = false;
            }

            foreach (string line in lines)
            {
                if (line.StartsWith("diff --git "))
                {
                    Finish();
                    continue;
                }

                if (!inHunk && line.StartsWith("+++ "))
                {
                    path = line.StartsWith("+++ b/") ? line.Substring(6) : null;
                    continue;
                }

                var header = HunkHeader.Match(line);
                if (header.Success)
                {
                    newLine = int.Parse(header.Groups[1].Value);
                    if (inHunk) sb.AppendLine("      ...");
                    inHunk = true;
                    continue;
                }

                if (!inHunk || path == null) continue;

                if (line.StartsWith("+"))
                {
                    sb.AppendLine($"{newLine,5} + {line.Substring(1)}");
                    addedNumbers.Add(newLine);
                    newLine++;
                    added++;
                }
                else if (line.StartsWith("-"))
                {
                    sb.AppendLine($"      - {line.Substring(1)}");
                }
                else if (line.StartsWith(" "))
                {
                    sb.AppendLine($"{newLine,5}   {line.Substring(1)}");
                    newLine++;
                }
            }

            Finish();
            return files;
        }

        // Packs files into text chunks that each fit one model call (roughly 4 characters per token)
        public List<string> Chunk(List<FileDiff> files, int maxTokensPerChunk = 3000)
        {
            int maxChars = maxTokensPerChunk * 4;
            var chunks = new List<string>();
            var current = new StringBuilder();

            foreach (var file in files)
            {
                string header = $"=== FILE: {file.Path} ===\n";
                string patch = file.AnnotatedPatch;
                int room = maxChars - header.Length;

                // One huge file must not blow the budget: cut it at a line boundary
                if (patch.Length > room)
                {
                    int cut = patch.LastIndexOf('\n', room);
                    patch = patch.Substring(0, cut > 0 ? cut : room)
                            + "\n[... rest of this file omitted: too large ...]";
                }

                string block = header + patch + "\n\n";

                if (current.Length > 0 && current.Length + block.Length > maxChars)
                {
                    chunks.Add(current.ToString());
                    current.Clear();
                }
                current.Append(block);
            }

            if (current.Length > 0) chunks.Add(current.ToString());
            return chunks;
        }
    }
}
