using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace MapsetChecks.checks.standard.spread
{
    [Check]
    public class CheckShortSliders : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                Beatmap.Mode.Standard
            },
            Difficulties = new Beatmap.Difficulty[]
            {
                Beatmap.Difficulty.Easy
            },
            Category = "Spread",
            Message = "Too short sliders.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing slider head and tail from being too close in time for easy difficulties."
                },
                {
                    "Reasoning",
                    @"
                    Newer players need time to comprehend when to hold down and let go of sliders. If a slider ends too quickly, 
                    the action of pressing the slider and very shortly afterwards letting it go will sometimes be difficult to 
                    handle. The action of lifting a key is similar in difficulty to pressing a key for newer players. So any 
                    distance in time you wouldn't place circles apart, you shouldn't place slider head and tail apart either."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Too Short",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} {1} ms, expected at least {2}.",
                        "timestamp - ", "duration", "threshold")
                    .WithCause(
                        "A slider in an Easy difficulty is less than 125 ms (240 bpm 1/2).") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            // Shortest length before warning is 1/2 at 240 BPM, 125 ms.
            double timeThreshold = 125;

            foreach (Slider slider in beatmap.hitObjects.OfType<Slider>())
                if (slider.endTime - slider.time < timeThreshold)
                    yield return new Issue(GetTemplate("Too Short"), beatmap,
                        Timestamp.Get(slider),
                        $"{slider.endTime - slider.time:0.##}",
                        timeThreshold);
        }
    }
}
