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
                    notably unsnapped from any distinct sound is still probably something you'd want to fix. Taiko has stronger
                    kiai screen effects so this matters a bit more for that mode."
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
                        "An inherited line with kiai enabled is unsnapped by 10 ms or more. For taiko this is 5 ms or more instead.") },

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

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (TimingLine line in beatmap.timingLines.Where(line => line.kiai))
            {
                // If we're inside of kiai, a new line with kiai won't cause kiai to start again.
                if (beatmap.GetTimingLine(line.offset - 1).kiai)
                    continue;

                double unsnap = beatmap.GetPracticalUnsnap(line.offset);

                // In taiko the screen changes color more drastically, so timing is more noticable.
                int warningThreshold = beatmap.generalSettings.mode == Beatmap.Mode.Taiko ? 5 : 10;
                if (Math.Abs(unsnap) >= warningThreshold)
                    yield return new Issue(GetTemplate("Warning"), beatmap,
                        Timestamp.Get(line.offset), unsnap);

                else if (Math.Abs(unsnap) >= 1)
                    yield return new Issue(GetTemplate("Minor"), beatmap,
                        Timestamp.Get(line.offset), unsnap);
                
                // Prevents duplicate issues occuring from both red and green line on same tick picking next line.
                if (beatmap.timingLines.Any(
                        otherLine => otherLine.offset == line.offset &&
                        !otherLine.uninherited && line.uninherited))
                    continue;

                TimingLine nextLine = beatmap.GetNextTimingLine(line.offset);
                if (nextLine == null || nextLine.kiai)
                    continue;

                unsnap = beatmap.GetPracticalUnsnap(nextLine.offset);

                if (Math.Abs(unsnap) >= 1)
                    yield return new Issue(GetTemplate("Minor End"), beatmap,
                        Timestamp.Get(nextLine.offset), unsnap);
            }
        }
    }
}
