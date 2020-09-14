using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MapsetChecks.checks.timing
{
    [Check]
    public class CheckPreview : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Timing",
            Message = "Inconsistent or unset preview time.",
            Author = "Naxess",
            
            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring that preview times are set and consistent for all beatmaps in the set."
                },
                {
                    "Reasoning",
                    @"
                    Without a set preview time the game will automatically pick a point to use as preview, but this rarely aligns with 
                    any beat or start of measure in the song. Additionally, not selecting a preview point will cause the web to use the 
                    whole song as preview, rather than the usual 10 second limit. Which difficulty is used to take preview time from is 
                    also not necessarily consistent between the web and the client.
                    <br \><br \>
                    Similarly to metadata and timing, preview points should really just be a global setting for the whole beatmapset and 
                    not difficulty-specific."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Not Set",
                    new IssueTemplate(Issue.Level.Problem,
                        "Preview time is not set.")
                    .WithCause(
                        "The preview time of a beatmap is missing.") },

                { "Inconsistent",
                    new IssueTemplate(Issue.Level.Problem,
                        "Preview time is inconsistent, see {0}.",
                        "difficulty")
                    .WithCause(
                        "The preview time of a beatmap is different from the reference beatmap.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            Beatmap refBeatmap = beatmapSet.beatmaps[0];
            foreach (Beatmap beatmap in beatmapSet.beatmaps)
            {
                if (beatmap.generalSettings.previewTime == -1)
                    yield return new Issue(GetTemplate("Not Set"), beatmap);

                else if (beatmap.generalSettings.previewTime != refBeatmap.generalSettings.previewTime)
                    yield return new Issue(GetTemplate("Inconsistent"), beatmap,
                        refBeatmap);
            }
        }
    }
}
