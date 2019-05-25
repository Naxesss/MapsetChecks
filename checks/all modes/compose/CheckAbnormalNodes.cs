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
    public class CheckAbnormalNodes : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Compose",
            Message = "Abnormal amount of slider nodes.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing mappers from writing inappropriate or otherwise harmful messages using slider nodes.
                    <image-right>
                        assets/docs/ohno.jpg
                        An example of text being written with slider nodes in a way which can easily be hidden offscreen.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    The code of conduct applies to all aspects of the ranking process, including the beatmap content itself, 
                    whether that only be visible through the editor or in gameplay as well."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Abnormal",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Slider contains {1} nodes.",
                        "timestamp - ", "amount")
                    .WithCause(
                        "A slider contains more nodes than 10 times the square root of its length in pixels.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            foreach (HitObject hitObject in aBeatmap.hitObjects)
                if (hitObject is Slider slider && slider.nodePositions.Count > 10 * Math.Sqrt(slider.pixelLength))
                    yield return new Issue(GetTemplate("Abnormal"), aBeatmap,
                        Timestamp.Get(slider), slider.nodePositions.Count);
        }
    }
}
