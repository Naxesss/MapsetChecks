using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MapsetChecks.checks.general.resources
{
    public class CheckVideoOffset : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Resources",
            Message = "Inconsistent video offset.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Multiple",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0}",
                        "video offset : difficulties")
                    .WithCause(
                        "There is more than one video offset used between all difficulties.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            foreach (Issue issue in Common.GetInconsistencies(
                aBeatmapSet,
                aBeatmap => aBeatmap.videos.Count > 0 ? aBeatmap.videos[0].offset.ToString() : null,
                GetTemplate("Multiple")))
            {
                yield return issue;
            }
        }
    }
}
