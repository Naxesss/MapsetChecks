using MapsetParser.objects;
using MapsetParser.settings;
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
    public class CheckInconsistentMetadata : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Metadata",
            Message = "Inconsistent metadata.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Tags",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "Inconsistent tags between {0} and {1}, difference being \"{2}\".",
                        "difficulty", "difficulty", "difference")
                    .WithCause(
                        "A tag is present in one difficulty but missing in another." +
                        "<note>Does not care which order the tags are written in or about duplicate tags, " +
                        "simply that the tags themselves are consistent.</note>") },

                { "Other Field",
                    new IssueTemplate(Issue.Level.Unrankable,
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
                issues.AddRange(TryAddIssue("artist",         beatmap, refBeatmap, aBeatmap => aBeatmap.metadataSettings.artist));
                issues.AddRange(TryAddIssue("unicode artist", beatmap, refBeatmap, aBeatmap => aBeatmap.metadataSettings.artistUnicode));
                issues.AddRange(TryAddIssue("title",          beatmap, refBeatmap, aBeatmap => aBeatmap.metadataSettings.title));
                issues.AddRange(TryAddIssue("unicode title",  beatmap, refBeatmap, aBeatmap => aBeatmap.metadataSettings.titleUnicode));
                issues.AddRange(TryAddIssue("source",         beatmap, refBeatmap, aBeatmap => aBeatmap.metadataSettings.source));
                issues.AddRange(TryAddIssue("creator",        beatmap, refBeatmap, aBeatmap => aBeatmap.metadataSettings.creator));
                foreach (Issue issue in issues)
                    yield return issue;
                
                if (beatmap.metadataSettings.tags != refSettings.tags)
                {
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
        }
        
        private IEnumerable<Issue> TryAddIssue(string aField, Beatmap aBeatmap, Beatmap anOtherBeatmap, Func<Beatmap, string> aMetadataField)
        {
            string field      = aMetadataField(aBeatmap);
            string otherField = aMetadataField(anOtherBeatmap);

            if (field != otherField)
                yield return
                    new Issue(GetTemplate("Other Field"), null,
                        aField,
                        aBeatmap, anOtherBeatmap,
                        field, otherField);
        }
    }
}
