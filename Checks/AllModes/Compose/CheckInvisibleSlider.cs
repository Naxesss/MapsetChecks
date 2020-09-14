using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.compose
{
    [Check]
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
                        https://i.imgur.com/xJIwdbA.png
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
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} has no slider nodes.",
                        "timestamp - ")
                    .WithCause(
                        "A slider has no nodes.") },

                { "Negative Length",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} has negative pixel length.",
                        "timestamp - ")
                    .WithCause(
                        "A slider has a negative pixel length.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (Slider slider in beatmap.hitObjects.OfType<Slider>())
                if (slider.nodePositions.Count == 0)
                    yield return new Issue(GetTemplate("Zero Nodes"), beatmap,
                        Timestamp.Get(slider));
                else if (slider.pixelLength < 0)
                    yield return new Issue(GetTemplate("Negative Length"), beatmap,
                        Timestamp.Get(slider));
        }
    }
}
