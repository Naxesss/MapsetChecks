using MapsetParser.objects;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MapsetChecks.checks.general.metadata
{
    public class CheckAdditionalMarkers : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Metadata",
            Message = "Additional markers in title.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing beatmapsets from adding unofficial markers in the song metadata, without having changed the song significantly 
                    like speeding it up, doing a DnB edit, or similar.
                    <image>
                        assets/docs/cutVer.jpg
                        A song which has been cut should not contain any marker for it like the ""cut ver."" here.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Songs which cut a portion of the original, or only slightly modifies it, should not have any markers, as this helps 
                    differentiate between official cuts or edits and unofficial ones. It also ensures that metadata does not become cluttered 
                    or overly inconsistent."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Romanized",
                    new IssueTemplate(Issue.Level.Warning,
                        "Romanized title field, \"{0}\"",
                        "romanized title")
                    .WithCause(
                        "The romanized title field indicates some kind of version.") },

                { "Unicode",
                    new IssueTemplate(Issue.Level.Warning,
                        "Unicode title field, \"{0}\"",
                        "unicode title")
                    .WithCause(
                        "Same as for romanized, but unicode instead.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            Beatmap beatmap = aBeatmapSet.beatmaps[0];

            // Matches additional markers, like "Speed up ver." and "Full Version".
            Regex regex = new Regex("(?i)(( |-)ver(\\.|sion)?)(\\))?$");

            if (regex.IsMatch(beatmap.metadataSettings.title))
                yield return new Issue(GetTemplate("Romanized"), null,
                    beatmap.metadataSettings.title);

            // Unicode fields do not exist in file version 9.
            if (beatmap.metadataSettings.titleUnicode != null &&
                regex.IsMatch(beatmap.metadataSettings.titleUnicode))
            {
                yield return new Issue(GetTemplate("Unicode"), null,
                    beatmap.metadataSettings.titleUnicode);
            }
        }
    }
}
