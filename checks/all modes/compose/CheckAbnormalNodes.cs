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
            Author = "Naxess"
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
