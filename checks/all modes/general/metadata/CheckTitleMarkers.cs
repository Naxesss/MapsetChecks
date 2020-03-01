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
    public class CheckTitleMarkers : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Metadata",
            Message = "Incorrect format of (TV Size) / (Game Ver.) / (Short Ver.) / (Sped Up Ver.) in title.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Standardizing the way metadata is written for ranked content.
                    <image>
                        https://i.imgur.com/1ozV71n.png
                        A song using ""-TV version-"" as its official metadata, which becomes ""(TV Size)"" when standardized.
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
            return new Dictionary<string, IssueTemplate>()
            {
                { "TV Size",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} title field; \"{1}\" incorrect format of \"(TV Size)\".",
                        "Romanized/unicode", "field")
                    .WithCause(
                        "The format of \"(TV Size)\" in either the romanized or unicode title is incorrect.") },

                { "Game Ver",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} title field; \"{1}\" incorrect format of \"(Game Ver.)\".",
                        "Romanized/unicode", "field")
                    .WithCause(
                        "The format of \"(Game Ver.)\" in either the romanized or unicode title is incorrect.") },

                { "Short Ver",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} title field; \"{1}\" incorrect format of \"(Short Ver.)\".",
                        "Romanized/unicode", "field")
                    .WithCause(
                        "The format of \"(Short Ver.)\" in either the romanized or unicode title is incorrect.") },

                { "Sped Up Ver",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} title field; \"{1}\" incorrect format of \"(Sped Up Ver.)\".",
                        "Romanized/unicode", "field")
                    .WithCause(
                        "The format of \"(Sped Up Ver.)\" in either the romanized or unicode title is incorrect.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            Beatmap beatmap = beatmapSet.beatmaps[0];
            
            // Matches any string containing some form of TV Size but not exactly "(TV Size)".
            Regex tvSizeRegex = new Regex(@"(?i)(tv.(size|ver))");
            Regex tvSizeExactRegex = new Regex(@"\(TV Size\)");

            foreach (Issue issue in GetIssuesFromRegex(beatmap, tvSizeRegex, tvSizeExactRegex, "TV Size"))
                yield return issue;
            
            Regex gameVerRegex = new Regex(@"(?i)(game.(size|ver))");
            Regex gameVerExactRegex = new Regex(@"\(Game Ver\.\)");

            foreach (Issue issue in GetIssuesFromRegex(beatmap, gameVerRegex, gameVerExactRegex, "Game Ver"))
                yield return issue;
            
            Regex shortVerRegex = new Regex(@"(?i)((short|cut).(size|ver))");
            Regex shortVerExactRegex = new Regex(@"\(Short Ver\.\)");

            foreach (Issue issue in GetIssuesFromRegex(beatmap, shortVerRegex, shortVerExactRegex, "Short Ver"))
                yield return issue;

            Regex spedVerRegex = new Regex(@"(?i)(sped|speed) ?up ver");
            Regex spedVerExactRegex = new Regex(@"\(Sped Up Ver\.\)");

            foreach (Issue issue in GetIssuesFromRegex(beatmap, spedVerRegex, spedVerExactRegex, "Sped Up Ver"))
                yield return issue;
        }

        /// <summary> Returns issues wherever the romanized or unicode title contains the regular regex but not the exact regex. </summary>
        private IEnumerable<Issue> GetIssuesFromRegex(Beatmap beatmap, Regex regex, Regex exactRegex, string templateName)
        {
            if (regex.IsMatch(beatmap.metadataSettings.title) &&
                !exactRegex.IsMatch(beatmap.metadataSettings.title))
            {
                yield return new Issue(GetTemplate(templateName), null,
                    "Romanized", beatmap.metadataSettings.title);
            }

            // Unicode fields do not exist in file version 9.
            if (beatmap.metadataSettings.titleUnicode != null
                && regex.IsMatch(beatmap.metadataSettings.titleUnicode) &&
                !exactRegex.IsMatch(beatmap.metadataSettings.titleUnicode))
            {
                yield return new Issue(GetTemplate(templateName), null,
                    "Unicode", beatmap.metadataSettings.titleUnicode);
            }
        }
    }
}
