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
    public class CheckFirstLine : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Timing",
            Message = "First line toggles kiai or is inherited.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Inherited",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} First timing line is inherited.",
                        "timestamp - ")
                    .WithCause(
                        "The first timing line of a beatmap is inherited.") },

                { "Toggles Kiai",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} First timing line toggles kiai.",
                        "timestamp - ")
                    .WithCause(
                        "The first timing line of a beatmap has kiai enabled.") },

                { "No Lines",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "There are no timing lines.")
                    .WithCause(
                        "A beatmap has no timing lines.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            if (aBeatmap.timingLines.Count > 0)
            {
                TimingLine line = aBeatmap.timingLines[0];

                if (!line.uninherited)
                    yield return new Issue(GetTemplate("Inherited"), aBeatmap, Timestamp.Get(line.offset));
                else if (line.kiai)
                    yield return new Issue(GetTemplate("Toggles Kiai"), aBeatmap, Timestamp.Get(line.offset));
            }
            else
                yield return new Issue(GetTemplate("No Lines"), aBeatmap);
        }
    }
}
