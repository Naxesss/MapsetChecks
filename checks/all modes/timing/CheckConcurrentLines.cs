using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MapsetChecks.checks.timing
{
    public class CheckConcurrentLines : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Timing",
            Message = "Two inherited or uninherited concurrent timing lines.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing issues with concurrent lines of the same type, such as them switching order when loading the beatmap.
                    <image>
                        https://i.imgur.com/whTV4aV.png
                        Two inherited lines which were originally the other way around, but swapped places when opening the beatmap again.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Depending on how the game loads the lines, they may be loaded in the wrong order causing certain effects to disappear, 
                    like the editor to not see that kiai is enabled where it is in gameplay. This coupled with the fact that future versions 
                    of the game may change how these behave make them highly unreliable.
                    <note>
                        Two lines of different types, however, work properly as inherited and uninherited lines are handeled seperately, 
                        where the inherited will always apply its effects last.
                    </note>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Concurrent",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} Concurrent {1} lines.",
                        "timestamp - ", "inherited/uninherited")
                    .WithCause(
                        "Two inherited or uninherited timing lines exist at the same point in time.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            // Since the list of timing lines is sorted by time we can just check the previous line.
            for (int i = 1; i < aBeatmap.timingLines.Count; ++i)
            {
                if (aBeatmap.timingLines[i - 1].offset == aBeatmap.timingLines[i].offset &&
                    aBeatmap.timingLines[i - 1].uninherited == aBeatmap.timingLines[i].uninherited)
                {
                    string inheritance =
                        aBeatmap.timingLines[i].uninherited ?
                            "uninherited" : "inherited";

                    yield return new Issue(GetTemplate("Concurrent"), aBeatmap,
                        Timestamp.Get(aBeatmap.timingLines[i].offset), inheritance);
                }
            }
        }
    }
}
