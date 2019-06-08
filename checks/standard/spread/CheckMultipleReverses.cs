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
using System.Linq;
using System.Numerics;

namespace MapsetChecks.checks.standard.spread
{
    public class CheckMultipleReverses : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                Beatmap.Mode.Standard
            },
            Difficulties = new Beatmap.Difficulty[]
            {
                Beatmap.Difficulty.Easy,
                Beatmap.Difficulty.Normal
            },
            Category = "Spread",
            Message = "Multiple reverses on too short sliders.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing sliders from having multiple reverses in easy and normal difficulties."
                },
                {
                    "Reasoning",
                    @"
                    Assuming we do this on a short slider, the reverse would be visible for so short of a time that it 
                    would be difficult to react to. If we instead do this on a long slider where they can react, it's 
                    going to be really boring gameplay-wise due to how long the slider becomes. So no matter how it's 
                    done, it'll be worse than alternatives."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Unrankable",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} This slider is way too short to have multiple reverses.",
                        "timestamp - ")
                    .WithCause(
                        "A slider has at least 2 reverses and is 250 ms or shorter (240 bpm 1/1) in an Easy, " +
                        "or 125 ms or shorter (240 bpm 1/2) in a Normal.") },

                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} This slider is very short to have multiple reverses.",
                        "timestamp - ")
                    .WithCause(
                        "A slider has at least 2 reverses and is 333 ms or shorter (180 bpm 1/1) in an Easy, " +
                        "or 167 ms or shorter (180 bpm 1/2) in a Normal.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            double rankableThreshold = 60000 / 240d;
            double warningThreshold  = 60000 / 180d;

            foreach (Slider slider in aBeatmap.hitObjects.OfType<Slider>())
            {
                if (slider.edgeAmount > 2)
                {
                    // 1/1 for Easy
                    string easyTemplate =
                        slider.endTime - slider.time < rankableThreshold ? "Unrankable" :
                        slider.endTime - slider.time < warningThreshold ? "Warning" :
                        null;

                    if (easyTemplate != null)
                        yield return new Issue(GetTemplate(easyTemplate), aBeatmap, Timestamp.Get(slider))
                            .WithInterpretation("difficulty", (int)Beatmap.Difficulty.Easy);

                    // 1/2 for Normal
                    string normalTemplate =
                        slider.endTime - slider.time < rankableThreshold * 0.5 ? "Unrankable" :
                        slider.endTime - slider.time < warningThreshold * 0.5 ? "Warning" :
                        null;

                    if (normalTemplate != null)
                        yield return new Issue(GetTemplate(normalTemplate), aBeatmap, Timestamp.Get(slider))
                            .WithInterpretation("difficulty", (int)Beatmap.Difficulty.Normal);
                }
            }
        }
    }
}
