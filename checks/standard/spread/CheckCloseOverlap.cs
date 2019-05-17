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
                        "{0} {1} ms apart, should either be overlapped or at least {2} ms apart.",
                        "timestamp - ", "gap", "threshold")
                    .WithCause(
                        "Two objects with a time gap less than 125 ms (240 bpm 1/2) are not overlapping.") },

                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} {1} ms apart.",
                        "timestamp - ", "gap")
                    .WithCause(
                        "Two objects with a time gap less than 167 ms (180 bpm 1/2) are not overlapping.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            double unrankableThreshold = 125; // Shortest acceptable gap is 1/2 in 240 BPM, 125 ms.
            double warningThreshold    = 375; // Shortest gap before warning is 1/2 in 160 BPM, 375 ms.

            for (int i = 0; i < aBeatmap.hitObjects.Count - 1; ++i)
            {
                HitObject hitObject     = aBeatmap.hitObjects[i];
                HitObject nextHitObject = aBeatmap.hitObjects[i + 1];

                // Slider ends do not need to overlap, same with spinners, spinners should be ignored overall.
                if (hitObject is Circle &&
                    !(nextHitObject is Spinner) &&
                    nextHitObject.time - hitObject.time < warningThreshold)
                {
                    double distance =
                        Math.Sqrt(
                            Math.Pow(hitObject.Position.X - nextHitObject.Position.X, 2) +
                            Math.Pow(hitObject.Position.Y - nextHitObject.Position.Y, 2));

                    // If the distance is larger or equal to two radiuses, then they're not overlapping.
                    float radius = aBeatmap.difficultySettings.GetCircleRadius();
                    if (distance >= radius * 2)
                    {
                        if(nextHitObject.time - hitObject.time < unrankableThreshold)
                            yield return new Issue(GetTemplate("Unrankable"), aBeatmap,
                                Timestamp.Get(hitObject, nextHitObject),
                                (Math.Round((nextHitObject.time - hitObject.time) * 100) / 100).ToString(CultureInfo.InvariantCulture),
                                unrankableThreshold);

                        else
                            yield return new Issue(GetTemplate("Warning"), aBeatmap,
                                Timestamp.Get(hitObject, nextHitObject),
                                (Math.Round((nextHitObject.time - hitObject.time) * 100) / 100).ToString(CultureInfo.InvariantCulture));
                    }
                }
            }
        }
    }
}
