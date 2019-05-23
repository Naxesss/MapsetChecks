using MapsetChecks.objects;
using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.compose
{
    public class CheckDrainTime : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Compose",
            Message = "Too short drain time.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Unrankable",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "Less than 30 seconds of drain time, currently {0}.",
                        "drain time")
                    .WithCause(
                        "The time from the first object to the end of the last object, subtracting any time between two objects " +
                        "where a break exists, is in total less than 30 seconds.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            if (aBeatmap.GetDrainTime() < 30 * 1000)
                yield return new Issue(GetTemplate("Unrankable"), aBeatmap,
                    Timestamp.Get(aBeatmap.GetDrainTime()));
        }
    }
}
