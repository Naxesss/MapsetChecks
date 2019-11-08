using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MapsetChecks.checks.timing
{
    [Check]
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
                        "Same as the other check, but by 1 ms or more instead.") },

                { "Minor End",
                    new IssueTemplate(Issue.Level.Minor,
                        "{0} Kiai end is unsnapped by {1} ms.",
                        "timestamp - ", "unsnap")
                    .WithCause(
                        "Same as the second check, except looks for where kiai ends.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            foreach (TimingLine line in aBeatmap.timingLines.Where(aLine => aLine.kiai))
            {
                // If we're inside of kiai, a new line with kiai won't cause kiai to start again.
                if (aBeatmap.GetTimingLine(line.offset - 1).kiai)
                    continue;

                double unsnap = aBeatmap.GetPracticalUnsnap(line.offset);

                if (Math.Abs(unsnap) >= 10)
                    yield return new Issue(GetTemplate("Warning"), aBeatmap,
                        Timestamp.Get(line.offset), unsnap);

                else if (Math.Abs(unsnap) >= 1)
                    yield return new Issue(GetTemplate("Minor"), aBeatmap,
                        Timestamp.Get(line.offset), unsnap);
                
                // Prevents duplicate issues occuring from both red and green line on same tick picking next line.
                if (aBeatmap.timingLines.Any(aLine => aLine.offset == line.offset && !aLine.uninherited && line.uninherited))
                    continue;

                TimingLine nextLine = aBeatmap.GetNextTimingLine(line.offset);
                if (nextLine != null && !nextLine.kiai)
                {
                    unsnap = aBeatmap.GetPracticalUnsnap(nextLine.offset);

                    if (Math.Abs(unsnap) >= 1)
                        yield return new Issue(GetTemplate("Minor End"), aBeatmap,
                            Timestamp.Get(nextLine.offset), unsnap);
                }
            }
        }
    }
}
