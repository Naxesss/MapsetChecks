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
    public class CheckPreview : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Timing",
            Message = "Inconsistent or unset preview time.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Not Set",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "Preview time is not set.")
                    .WithCause(
                        "The preview time of a beatmap is missing.") },

                { "Inconsistent",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "Preview time is inconsistent, see {0}.",
                        "difficulty")
                    .WithCause(
                        "The preview time of a beatmap is different from the reference beatmap.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            Beatmap refBeatmap = aBeatmapSet.beatmaps[0];
            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
            {
                if (beatmap.generalSettings.previewTime == -1)
                    yield return new Issue(GetTemplate("Not Set"), beatmap);

                if (beatmap.generalSettings.previewTime != refBeatmap.generalSettings.previewTime)
                    yield return new Issue(GetTemplate("Inconsistent"), beatmap,
                        refBeatmap);
            }
        }
    }
}
