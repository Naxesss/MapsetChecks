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
    public class CheckUnicode : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Metadata",
            Message = "Unicode in romanized fields.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring that all characters in the romanized metadata fields can be displayed and communicated properly across 
                    multiple operating systems, devices and internet protocols.
                    <image>
                        https://i.imgur.com/3UAwC97.png
                        A beatmap with its unicode title manually edited into its romanized title field.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    The romanized title, artist, creator and difficulty name fields are used in the file name of the .osu and .osb, as well as by 
                    the website to allow for updates and syncing. As such, if they contain invalid characters, the beatmapset may become corrupt 
                    when uploaded, preventing users from properly downloading it.
                    <br \><br \>
                    Even if it were possible to download correctly, should a character be unsupported it will be displayed as a box, questionmark 
                    or other placeholder character in-game, which makes some titles and artists impossible to interpret and distinguish.
                    <br \><br \>
                    Some unicode characters do seem to work, however. The title and artist fields are regulated by the Ranking Criteria, and 
                    creator names filtered upon creation, but difficulty names are not regulated or filtered by anything, so we do those case by 
                    case."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Problem",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} field contains unicode characters,\"{1}\", those being \"{2}\".",
                        "Artist/title/creator", "field", "unicode char(s)")
                    .WithCause(
                        "The romanized title, artist, or creator field contains unicode characters.") },

                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} field contains unicode characters,\"{1}\", those being \"{2}\". If the map can still be downloaded this is probably ok.",
                        "difficulty name", "field", "unicode char(s)")
                    .WithCause(
                        "The difficulty name field contains unicode characters.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (Beatmap beatmap in beatmapSet.beatmaps)
            {
                foreach (Issue issue in GetUnicodeIssues("Difficulty name", beatmap.metadataSettings.version, "Warning"))
                    yield return issue;

                foreach (Issue issue in GetUnicodeIssues("Romanized title", beatmap.metadataSettings.title))
                    yield return issue;

                foreach (Issue issue in GetUnicodeIssues("Romanized artist", beatmap.metadataSettings.artist))
                    yield return issue;

                foreach (Issue issue in GetUnicodeIssues("Creator", beatmap.metadataSettings.creator))
                    yield return issue;
            }
        }

        private IEnumerable<Issue> GetUnicodeIssues(string fieldName, string field, string template = "Problem")
        {
            if (ContainsUnicode(field))
                yield return new Issue(GetTemplate(template), null,
                    fieldName, field, GetUnicodeCharacters(field));
        }

        private bool   IsUnicode(char ch)               => ch > 127;
        private bool   ContainsUnicode(string str)      => str.Any(IsUnicode);
        private string GetUnicodeCharacters(string str) => String.Join("", str.Where(IsUnicode));
    }
}
