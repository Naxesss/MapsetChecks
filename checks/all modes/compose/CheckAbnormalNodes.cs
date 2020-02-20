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
                        https://i.imgur.com/rlCoEtZ.png
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

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (HitObject hitObject in beatmap.hitObjects)
                if (hitObject is Slider slider && slider.nodePositions.Count > 10 * Math.Sqrt(slider.pixelLength))
                    yield return new Issue(GetTemplate("Abnormal"), beatmap,
                        Timestamp.Get(slider), slider.nodePositions.Count);
        }
    }
}
