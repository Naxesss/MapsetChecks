using MapsetParser.objects;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MapsetChecks.checks.general.files
{
    public class CheckUnicode : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Metadata",
            Message = "Unicode in romanized fields.",
            Author = "Naxess"
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
