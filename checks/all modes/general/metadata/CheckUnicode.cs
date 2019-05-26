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
                        assets/docs/unicodeTitle.jpg
                        A beatmap with its unicode title manually edited into its romanized title field.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    The romanized title, artist and creator fields are used in the file name of the .osu and .osb, as well as by the website 
                    to allow for updates and syncing. As such, if they contain invalid characters, the beatmapset may become corrupt when uploaded, 
                    preventing users from properly downloading it.
                    <br \><br \>
                    Even if it were possible to download correctly, should a character be unsupported it will be displayed as a box, questionmark 
                    or other placeholder character in-game, which makes some titles and artists impossible to interpret and distinguish."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Unicode",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} field contains unicode characters,\"{1}\".",
                        "Artist/title/creator", "field")
                    .WithCause(
                        "The romanized title, artist or the creator field contains unicode characters.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
            {
                if (ContainsUnicode(beatmap.metadataSettings.title))
                    yield return new Issue(GetTemplate("Unicode"), null,
                        "Romanized title", beatmap.metadataSettings.title);

                if (ContainsUnicode(beatmap.metadataSettings.artist))
                    yield return new Issue(GetTemplate("Unicode"), null,
                        "Romanized artist", beatmap.metadataSettings.artist);

                if (ContainsUnicode(beatmap.metadataSettings.creator))
                    yield return new Issue(GetTemplate("Unicode"), null,
                        "Creator", beatmap.metadataSettings.creator);
            }
        }

        private bool ContainsUnicode(string aString) => aString.Any(aChar => aChar > 127);
    }
}
