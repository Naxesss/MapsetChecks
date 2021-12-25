using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MapsetParser.objects;
using MapsetParser.settings;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;

namespace MapsetChecks.Checks.AllModes.General.Metadata
{
    [Check]
    public class CheckMarkerFormat : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata
        {
            Category = "Metadata",
            Message = "Incorrect marker format.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Standardizing the way metadata is written for ranked content.
                    <image>
                        https://i.imgur.com/e5mHEan.png
                        An example of ""featured by"", which should be replaced by ""feat."".
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Small deviations in metadata or obvious mistakes in its formatting or capitalization are for the 
                    most part eliminated through standardization. Standardization also reduces confusion in case of 
                    multiple correct ways to write certain fields and contributes to making metadata more consistent 
                    across official content."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "Wrong Format",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} {1} field, \"{2}\".",
                        "Romanized/unicode", "artist/title", "field")
                    .WithCause(
                        "The artist or title field of a difficulty includes an incorrect format of \"CV:\", \"vs.\" or \"feat.\".") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            Beatmap refBeatmap = beatmapSet.beatmaps[0];

            // Matches strings which contain some version of "vs.", "CV:" or "feat." markers but not exactly.
            Regex regexVs   = new Regex(@"(?i)( vs\.?[^A-Za-z0-9])");
            Regex regexCV   = new Regex(@"(?i)((\(| |（)cv(:|：)?[^A-Za-z0-9])");
            Regex regexFeat = new Regex(@"(?i)((\(| |（)(ft|feat)\.?[^A-Za-z0-9])");

            Regex regexVsExact   = new Regex(@"vs\.");
            Regex regexCVExact   = new Regex(@"CV(:|：)");
            Regex regexFeatExact = new Regex(@"feat\.");

            foreach (Issue issue in GetFormattingIssues(refBeatmap.metadataSettings, field =>
                    regexVs.IsMatch(field) && !regexVsExact.IsMatch(field) ||
                    regexCV.IsMatch(field) && !regexCVExact.IsMatch(field) ||
                    regexFeat.IsMatch(field) && !regexFeatExact.IsMatch(field)))
                yield return issue;
        }

        /// <summary> Applies a predicate to all artist and title metadata fields. Yields an issue wherever the predicate is true. </summary>
        private IEnumerable<Issue> GetFormattingIssues(MetadataSettings settings, Func<string, bool> FieldPredicate)
        {
            if (FieldPredicate(settings.artist))
                yield return new Issue(GetTemplate("Wrong Format"), null,
                    "Romanized", "artist", settings.artist);

            // Unicode fields do not exist in file version 9.
            if (settings.artistUnicode != null && FieldPredicate(settings.artistUnicode))
                yield return new Issue(GetTemplate("Wrong Format"), null,
                    "Unicode", "artist", settings.artistUnicode);

            if (FieldPredicate(settings.title))
                yield return new Issue(GetTemplate("Wrong Format"), null,
                    "Romanized", "title", settings.title);

            if (settings.titleUnicode != null && FieldPredicate(settings.titleUnicode))
                yield return new Issue(GetTemplate("Wrong Format"), null,
                    "Unicode", "title", settings.titleUnicode);
        }
    }
}
