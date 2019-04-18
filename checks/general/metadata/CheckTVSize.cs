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
    public class CheckTVSize : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Metadata",
            Message = "Incorrect format of (TV Size) in title.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Wrong Format",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} title field; \"{1}\".",
                        "Romanized/unicode", "field")
                    .WithCause(
                        "The format of \"(TV Size)\" in either the romanized or unicode title is incorrect.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            Beatmap beatmap = aBeatmapSet.beatmaps[0];

            // Matches any string containing some form of TV Size but not exactly "(TV Size)".
            Regex regex = new Regex("(?i)(tv.(size|ver))");
            Regex exactRegex = new Regex("\\(TV Size\\)");

            if (regex.IsMatch(beatmap.metadataSettings.title) &&
                !exactRegex.IsMatch(beatmap.metadataSettings.title))
            {
                yield return new Issue(GetTemplate("Wrong Format"), null,
                    "Romanized", beatmap.metadataSettings.title);
            }

            // Unicode fields do not exist in file version 9.
            if (beatmap.metadataSettings.titleUnicode != null
                && regex.IsMatch(beatmap.metadataSettings.titleUnicode) &&
                !exactRegex.IsMatch(beatmap.metadataSettings.titleUnicode))
            {
                yield return new Issue(GetTemplate("Wrong Format"), null,
                    "Unicode", beatmap.metadataSettings.titleUnicode);
            }
        }
    }
}
