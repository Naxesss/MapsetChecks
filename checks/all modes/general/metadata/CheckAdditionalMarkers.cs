using MapsetParser.objects;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MapsetChecks.checks.general.metadata
{
    [Check]
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
                        https://i.imgur.com/QWNvp2i.png
                        A song which has been unofficially cut should not contain any marker for it, especially not ""cut ver."".
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
                        "The romanized title field indicates some kind of version or size. " +
                        "Ignores \"(TV Size)\", \"(Short Ver.)\" and \"(Game Ver.)\".") },

                { "Unicode",
                    new IssueTemplate(Issue.Level.Warning,
                        "Unicode title field, \"{0}\"",
                        "unicode title")
                    .WithCause(
                        "Same as for romanized, but unicode instead.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            Beatmap beatmap = beatmapSet.beatmaps[0];
            
            Regex regex      = new Regex(@"(?i)([^A-Za-z0-9]+)?(ver(\.|sion)?|size)([^A-Za-z0-9]+)?$");
            Regex shortRegex = new Regex(@"(\(|（)Short Ver\.(\)|）)$");
            Regex gameRegex  = new Regex(@"(\(|（)Game Ver\.(\)|）)$");
            Regex tvRegex    = new Regex(@"(\(|（)TV Size(\)|）)$");

            // Matches additional markers, like "(Speed up ver.)" and "- Full Version -".
            // Excludes any field with correct markers.
            bool IsMatch(string field) =>
                regex      .IsMatch(field) &&
                !shortRegex.IsMatch(field) &&
                !gameRegex .IsMatch(field) &&
                !tvRegex   .IsMatch(field);

            if (IsMatch(beatmap.metadataSettings.title))
                yield return new Issue(GetTemplate("Romanized"), null,
                    beatmap.metadataSettings.title);

            // Unicode fields do not exist in file version 9.
            if (beatmap.metadataSettings.titleUnicode != null &&
                IsMatch(beatmap.metadataSettings.titleUnicode))
            {
                yield return new Issue(GetTemplate("Unicode"), null,
                    beatmap.metadataSettings.titleUnicode);
            }
        }
    }
}
