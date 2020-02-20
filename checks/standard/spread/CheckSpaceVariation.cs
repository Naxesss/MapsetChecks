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
    public class CheckSpaceVariation : BeatmapCheck
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
            Message = "Object too close or far away from previous.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring spacing between objects with the same snapping is recognizable, and objects with different snappings are 
                    distinguishable, for easy and normal difficulties."
                },
                {
                    "Reasoning",
                    @"
                    Time distance equality is a fundamental concept used in low difficulties to teach newer players how to interpret 
                    rhythm easier. By trivializing reading, these maps can better teach how base mechanics work, like approach circles, 
                    slider follow circles, object fading, hit bursts, hit sounds, etc.
                    <br \><br \>
                    Once these are learnt, and by the time players move on to hard difficulties, more advanced concepts and elements 
                    can begin to be introduced, like multiple reverses, spacing as a form of emphasis, complex rhythms, streams, and so 
                    on."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Distance",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Distance is {1} px, expected {2}, see {3}.",
                        "timestamp - ", "distance", "distance", "example objects")
                    .WithCause(
                        "The distance between two hit objects noticeably contradicts a recent use of time distance balance between another " +
                        "two hit objects using a similar time gap.") },

                { "Ratio",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Distance/time ratio is {1}, expected {2}.",
                        "timestamp - ", "ratio", "ratio")
                    .WithCause(
                        "The distance/time ratio between the previous hit objects greatly contradicts a following use of distance/time ratio.") }
            };
        }

        private struct ObservedDistance
        {
            public double deltaTime;
            public double distance;
            public HitObject hitObject;

            public ObservedDistance(double deltaTime, double distance, HitObject hitObject)
            {
                this.deltaTime = deltaTime;
                this.distance = distance;
                this.hitObject = hitObject;
            }
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            HitObject nextObject;
            
            double deltaTime;
            
            List<ObservedDistance> observedDistances = new List<ObservedDistance>();
            ObservedDistance? observedIssue = null;

            double distanceExpected;
            double distance;

            double mleniencyPercent = 0.15;
            double leniencyAbsolute = 10;

            double snapLeniencyPercent = 0.1;

            double ratioLeniencyPercent = 0.2;
            double ratioLeniencyAbsolute = 0.1;

            foreach (HitObject hitObject in beatmap.hitObjects)
            {
                nextObject = beatmap.GetNextHitObject(hitObject.time);

                // Ignore spinners, since they have no clear start or end.
                if (hitObject is Spinner || nextObject is Spinner || nextObject == null)
                    continue;
                
                deltaTime = nextObject.GetPrevDeltaTime();

                // Ignore objects 2 beats or more apart (assuming 200 bpm), since they don't really hang together context-wise.
                if (deltaTime > 600)
                    continue;

                distance = nextObject.GetPrevDistance();

                // Ignore stacks and half-stacks, since these are relatively normal.
                if (distance < 8)
                    continue;

                double closeDistanceSum =
                    observedDistances.Sum(observedDistance =>
                        observedDistance.hitObject.time > hitObject.time - 4000 ?
                            observedDistance.distance / observedDistance.deltaTime : 0);
                int closeDistanceCount =
                    observedDistances.Count(observedDistance =>
                        observedDistance.hitObject.time > hitObject.time - 4000);
                
                double avrRatio = closeDistanceCount > 0 ? closeDistanceSum / closeDistanceCount : -1;

                // Checks whether a similar snapping has already been observed and uses that as
                // reference for determining if the current is too different.
                int index =
                    observedDistances
                        .FindLastIndex(observedDistance =>
                            deltaTime <= observedDistance.deltaTime * (1 + snapLeniencyPercent) &&
                            deltaTime >= observedDistance.deltaTime * (1 - snapLeniencyPercent) &&
                            observedDistance.hitObject.time > hitObject.time - 4000);

                if (index != -1)
                {
                    distanceExpected = observedDistances[index].distance;

                    if ((Math.Abs(distanceExpected - distance) - leniencyAbsolute) / distance > mleniencyPercent)
                    {
                        // Prevents issues from duplicating due to error being different compared to both before and after.
                        // (e.g. if 1 -> 2 is too large, and 2 -> 3 is only too small because of 1 -> 2 being an issue, we
                        // only mention 1 -> 2 rather than both, since they stem from the same issue)
                        double distanceExpectedAlternate = observedIssue?.distance ?? 0;

                        if (observedIssue != null
                            && Math.Abs(distanceExpectedAlternate - distance) / distance <= mleniencyPercent)
                        {
                            observedDistances[index] = new ObservedDistance(deltaTime, distance, hitObject);
                            observedIssue = null;
                        }
                        else
                        {
                            HitObject prevObject = observedDistances[index].hitObject;
                            HitObject prevNextObject = beatmap.GetNextHitObject(prevObject.time);

                            yield return new Issue(GetTemplate("Distance"), beatmap,
                                Timestamp.Get(hitObject, nextObject),
                                (int)Math.Round(distance), (int)Math.Round(distanceExpected),
                                Timestamp.Get(prevObject, prevNextObject));

                            observedIssue = new ObservedDistance(deltaTime, distance, hitObject);
                        }
                    }
                    else
                    {
                        observedDistances[index] = new ObservedDistance(deltaTime, distance, hitObject);
                        observedIssue = null;
                    }
                }
                else
                {
                    if (avrRatio != -1 && (
                        distance / deltaTime - ratioLeniencyAbsolute > avrRatio * (1 + ratioLeniencyPercent) ||
                        distance / deltaTime + ratioLeniencyAbsolute < avrRatio * (1 - ratioLeniencyPercent)))
                    {
                        string ratio         = $"{distance / deltaTime:0.##}";
                        string ratioExpected = $"{avrRatio:0.##}";

                        yield return new Issue(GetTemplate("Ratio"), beatmap,
                            Timestamp.Get(hitObject, nextObject),
                            ratio, ratioExpected);
                    }
                    else
                    {
                        observedDistances.Add(new ObservedDistance(deltaTime, distance, hitObject));
                        observedIssue = null;
                    }
                }
            }
        }
    }
}
