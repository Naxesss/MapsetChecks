using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MapsetChecks.checks.timing
{
    public class CheckKiaiUnsnap : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Timing",
            Message = "Unsnapped kiai.",
            Author = "Naxess",
            
            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring kiai starts on a distinct sound."
                },
                {
                    "Reasoning",
                    @"
                    Since kiai is visual, unlike hit sounds, it doesn't need to be as precise in timing, but kiai being 
                    notably unsnapped from any distinct sound is still probably something you'd want to fix."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Kiai is unsnapped by {1} ms.",
                        "timestamp - ", "unsnap")
                    .WithCause(
                        "An inherited line with kiai enabled is unsnapped by 10 ms or more.") },

                { "Minor",
                    new IssueTemplate(Issue.Level.Minor,
                        "{0} Kiai is unsnapped by {1} ms.",
                        "timestamp - ", "unsnap")
                    .WithCause(
                        "Same as the other check, but by 1 ms or more instead.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            foreach (TimingLine line in aBeatmap.timingLines)
            {
                double unsnap = aBeatmap.GetPracticalUnsnap(line.offset);

                if (Math.Abs(unsnap) >= 10)
                    yield return new Issue(GetTemplate("Warning"), aBeatmap,
                        Timestamp.Get(line.offset), unsnap);

                else if (Math.Abs(unsnap) >= 1)
                    yield return new Issue(GetTemplate("Minor"), aBeatmap,
                        Timestamp.Get(line.offset), unsnap);
            }
        }
    }
}
