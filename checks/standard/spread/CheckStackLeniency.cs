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

namespace MapsetChecks.checks.timing
{
    public class CheckStackLeniency : BeatmapCheck
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
                Beatmap.Difficulty.Normal,
                Beatmap.Difficulty.Hard,
                Beatmap.Difficulty.Insane,
            },
            Category = "Spread",
            Message = "Perfect stacks too close in time.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Unrankable",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} Stack leniency should be at least {1}.",
                        "timestamp - ", "stack leniency")
                    .WithCause(
                        "Two objects are overlapping perfectly and are less than 1/1, 1/1, 1/2, or 1/4 apart (assuming 160 BPM), for E/N/H/I respectively.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            double[] snapping = new double[] { 1, 1, 0.5, 0.25 };

            for (int diffIndex = 0; diffIndex < snapping.Length; ++diffIndex)
            {
                double timeGap = snapping[diffIndex] * 60000 / 160d;
                int requiredStackLeniency = (int)Math.Ceiling(timeGap / (aBeatmap.difficultySettings.GetPreemptTime() * 0.1));

                List<HitObject> iteratedObjects = new List<HitObject>();
                foreach (HitObject hitObject in aBeatmap.hitObjects)
                {
                    iteratedObjects.Add(hitObject);
                    foreach (HitObject otherHitObject in aBeatmap.hitObjects.Except(iteratedObjects))
                    {
                        if (hitObject.Position == otherHitObject.Position &&
                            !(hitObject is Spinner) && !(otherHitObject is Spinner) &&
                            otherHitObject.time - hitObject.time < timeGap)
                        {
                            yield return new Issue(GetTemplate("Unrankable"), aBeatmap,
                                Timestamp.Get(hitObject, otherHitObject), requiredStackLeniency)
                                .WithInterpretation("difficulty", diffIndex);
                        }
                    }
                }
            }
        }
    }
}
