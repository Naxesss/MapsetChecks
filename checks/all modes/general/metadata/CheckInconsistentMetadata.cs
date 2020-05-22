using MapsetParser.objects;
using MapsetParser.settings;
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
    public class CheckInconsistentMetadata : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Metadata",
            Message = "Inconsistent metadata.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Keeping metadata consistent between all difficulties of a beatmapset.
                    <image>
                        https://i.imgur.com/ojdxg6z.png
                        Comparing two difficulties with different titles in a beatmapset.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Since all difficulties are of the same song, they should use the same song metadata. The website also assumes it's all the 
                    same, so it'll only display one of the artists, titles, creators, etc. Multiple metadata simply isn't supported very well, 
                    and should really just be a global beatmap setting rather than a .osu-specific one."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Tags",
                    new IssueTemplate(Issue.Level.Problem,
                        "Inconsistent tags between {0} and {1}, difference being \"{2}\".",
                        "difficulty", "difficulty", "difference")
                    .WithCause(
                        "A tag is present in one difficulty but missing in another." +
                        "<note>Does not care which order the tags are written in or about duplicate tags, " +
                        "simply that the tags themselves are consistent.</note>") },

                { "Other Field",
                    new IssueTemplate(Issue.Level.Problem,
                        "Inconsistent {0} fields between {1} and {2}; \"{3}\" and \"{4}\" respectively.",
                        "field", "difficulty", "difficulty", "field", "field")
                    .WithCause(
                        "A metadata field is not consistent between all difficulties.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            Beatmap refBeatmap = beatmapSet.beatmaps[0];
            string refVersion = refBeatmap.metadataSettings.version;
            MetadataSettings refSettings = refBeatmap.metadataSettings;

            foreach (Beatmap beatmap in beatmapSet.beatmaps)
            {
                string curVersion = beatmap.metadataSettings.version;

                List<Issue> issues = new List<Issue>();
                issues.AddRange(GetInconsistency("artist",         beatmap, refBeatmap, otherBeatmap => otherBeatmap.metadataSettings.artist));
                issues.AddRange(GetInconsistency("unicode artist", beatmap, refBeatmap, otherBeatmap => otherBeatmap.metadataSettings.artistUnicode));
                issues.AddRange(GetInconsistency("title",          beatmap, refBeatmap, otherBeatmap => otherBeatmap.metadataSettings.title));
                issues.AddRange(GetInconsistency("unicode title",  beatmap, refBeatmap, otherBeatmap => otherBeatmap.metadataSettings.titleUnicode));
                issues.AddRange(GetInconsistency("source",         beatmap, refBeatmap, otherBeatmap => otherBeatmap.metadataSettings.source));
                issues.AddRange(GetInconsistency("creator",        beatmap, refBeatmap, otherBeatmap => otherBeatmap.metadataSettings.creator));
                foreach (Issue issue in issues)
                    yield return issue;

                if (beatmap.metadataSettings.tags == refSettings.tags)
                    continue;

                IEnumerable<string> refTags = refSettings.tags.Split(' ');
                IEnumerable<string> curTags = beatmap.metadataSettings.tags.Split(' ');
                IEnumerable<string> differenceTags = refTags.Except(curTags).Union(curTags.Except(refTags)).Distinct();

                string difference = String.Join(" ", differenceTags);
                if (difference != "")
                    yield return
                        new Issue(GetTemplate("Tags"), null,
                            curVersion, refVersion,
                            difference);
            }
        }
        
        /// <summary> Returns issues where the metadata fields of the given beatmaps do not match. </summary>
        private IEnumerable<Issue> GetInconsistency(string fieldName, Beatmap beatmap, Beatmap otherBeatmap, Func<Beatmap, string> MetadataField)
        {
            string field      = MetadataField(beatmap);
            string otherField = MetadataField(otherBeatmap);

            if ((field ?? "") != (otherField ?? ""))
                yield return
                    new Issue(GetTemplate("Other Field"), null,
                        fieldName,
                        beatmap, otherBeatmap,
                        field, otherField);
        }
    }
}
