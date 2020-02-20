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
    public class CheckSource : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Metadata",
            Message = "BMS used as source.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing BMS from being used in the source field."
                },
                {
                    "Reasoning",
                    @"
                    BMS is a file format used in the Beatmania series, and as such isn't an actual source. 
                    To allow it to still be found using this query, put it in the tags field instead and 
                    leave the source field blank (if there is no valid source)."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Problem",
                    new IssueTemplate(Issue.Level.Problem,
                        "Source field is \"{0}\".",
                        "field")
                    .WithCause(
                        "The source field is literally \"BMS\" case-insensitive.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            Beatmap refBeatmap = beatmapSet.beatmaps.FirstOrDefault();
            if (refBeatmap != null && refBeatmap.metadataSettings.source.ToLower() == "bms")
                yield return new Issue(GetTemplate("Problem"), null,
                    refBeatmap.metadataSettings.source);
        }
    }
}
