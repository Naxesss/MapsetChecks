using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MapsetChecks.checks.timing
{
    public class CheckConcurrentLines : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Timing",
            Message = "Two inherited or uninherited concurrent timing lines.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Concurrent",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} Concurrent {1} lines.",
                        "timestamp - ", "inherited/uninherited")
                    .WithCause(
                        "Two inherited or uninherited timing lines exist at the same point in time.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            // Since the list of timing lines is sorted by time we can just check the previous line.
            for (int i = 1; i < aBeatmap.timingLines.Count; ++i)
            {
                if (aBeatmap.timingLines[i - 1].offset == aBeatmap.timingLines[i].offset &&
                    aBeatmap.timingLines[i - 1].uninherited == aBeatmap.timingLines[i].uninherited)
                {
                    string inheritance = (aBeatmap.timingLines[i].uninherited ? "uninherited" : "inherited");
                    yield return new Issue(GetTemplate("Concurrent"), aBeatmap,
                        Timestamp.Get(aBeatmap.timingLines[i].offset), inheritance);
                }
            }
        }
    }
}
