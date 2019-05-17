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
    public class CheckCloseOverlap : BeatmapCheck
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
            Message = "Objects close in time not overlapping.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Unrankable",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0}",
                        "timestamp - ")
                    .WithCause(
                        "Two objects with a time gap less than 125 ms (240 bpm 1/2) are not overlapping.") },

                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0}",
                        "timestamp - ")
                    .WithCause(
                        "Two objects with a time gap less than 250 ms (120 bpm 1/2, 240 BPM 1/1) are not overlapping.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            // 1/2 in 240 BPM becomes 125 ms for 1/2 and 250 ms for 1/1.
            // 1/2 in 180 BPM becomes 167 ms for 1/2 and 333 ms for 1/1.
            // 1/2 in 120 BPM becomes 250 ms for 1/2 and 500 ms for 1/1.

            // We need a BPM independent variable within this range that best determines if something is 1/2.
            // If the delta time is less than 250 ms, we say it's unrankable (240 BPM is double-time).
            // If the delta time is less than 500 ms, we warn (120 BPM is half-time).

            // pishi said to use 120 BPM as leniency

            double unrankableThreshold = 125;
            double warningThreshold    = 250;

            for (int i = 0; i < aBeatmap.hitObjects.Count - 1; ++i)
            {
                HitObject hitObject = aBeatmap.hitObjects[i];
                HitObject nextHitObject = aBeatmap.hitObjects[i + 1];

                // slider ends do not need to overlap, same with spinners, spinners should be ignored overall
                // ensure objects are close enough in time that they need to overlap
                if (hitObject is Circle && !(nextHitObject is Spinner) &&
                    nextHitObject.time - hitObject.time < warningThreshold)
                {
                    double distance =
                        Math.Sqrt(
                            Math.Pow(hitObject.Position.X - nextHitObject.Position.X, 2) +
                            Math.Pow(hitObject.Position.Y - nextHitObject.Position.Y, 2));

                    // if the distance is larger or equal to two radiuses, then they're not overlapping
                    float radius = aBeatmap.difficultySettings.GetCircleRadius();
                    if (distance >= radius * 2)
                    {
                        if(nextHitObject.time - hitObject.time < unrankableThreshold)
                            yield return new Issue(GetTemplate("Unrankable"), aBeatmap,
                                Timestamp.Get(hitObject, nextHitObject));

                        else
                            yield return new Issue(GetTemplate("Warning"), aBeatmap,
                                Timestamp.Get(hitObject, nextHitObject));
                    }
                }
            }
        }
    }
}
