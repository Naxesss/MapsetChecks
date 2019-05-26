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
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring that the video aligns with the song consistently for all difficulties.
                    <image>
                        assets/docs/videoOffset.jpg
                        Two difficulties with different video offsets, as shown in the respective .osu files. The second 
                        argument, after ""Video"", is the offset in ms.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Since many videos tend to match the music in some way, for example do transitions on downbeats, it wouldn't 
                    make much sense having difficulty-dependent video offsets, as all difficulties are based around the same song 
                    starting at the same point in time."
                }
            }
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
