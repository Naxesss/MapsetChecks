using MapsetChecks.objects;
using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.compose
{
    public class CheckInvisibleSlider : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Compose",
            Message = "Invisible sliders.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing objects from being invisible.
                    <image-right>
                        assets/docs/zeroNode.jpg
                        A slider with no nodes; looks like a circle on the timeline but is invisible on the playfield.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Although often used in combination with a storyboard to make up for the invisiblity through sprites, there 
                    is no way to force the storyboard to appear, meaning players may play the map unaware that they should have 
                    enabled something for a fair gameplay experience."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Zero Nodes",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} has no slider nodes.",
                        "timestamp - ")
                    .WithCause(
                        "A slider has no nodes.") },

                { "Negative Length",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} has negative pixel length.",
                        "timestamp - ")
                    .WithCause(
                        "A slider has a negative pixel length.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            foreach (Slider slider in aBeatmap.hitObjects.OfType<Slider>())
                if (slider.nodePositions.Count == 0)
                    yield return new Issue(GetTemplate("Zero Nodes"), aBeatmap,
                        Timestamp.Get(slider));
                else if (slider.pixelLength < 0)
                    yield return new Issue(GetTemplate("Negative Length"), aBeatmap,
                        Timestamp.Get(slider));
        }
    }
}
