using System;
using System.Collections.Generic;
using System.Linq;
using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;

namespace MapsetChecks.Checks.Standard.Compose
{
    [Check]
    public class CheckAbnormalSpacing : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Modes = new[]
            {
                Beatmap.Mode.Standard
            },
            Category = "Compose",
            Message = "Abnormally large spacing.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Prevent the time/distance ratio of hit objects from being absurdly large, even for higher difficulties.
                    This is often a cause of an object being snapped a 1/4th tick earlier, and has been a common reason for unranks.
                    "
                },
                {
                    "Reasoning",
                    @"
                    With two objects being spaced way further than previous objects of the same snapping, it can be extremely
                    difficult to expect, much less play.
                    <br><br>
                    When combined with incorrect snappings (which abnormal spacing is often a cause of), this can really throw
                    players off to the point where the map is pretty much considered unplayable.
                    "
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "Problem",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} Space/time ratio is {1} times the expected, see e.g. {2}.",
                        "timestamp - ", "times", "example objects")
                    .WithCause(
                        @"
                        The space/time ratio between two objects is absurdly large in comparison to other objects with the same snapping prior.
                        <note>
                            Accounts for slider leniency by assuming that the gap is a circle's diameter smaller.
                        </note>
                        ") },

                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Space/time ratio is {1} times the expected, see e.g. {2}.",
                        "timestamp - ", "times", "example objects")
                    .WithCause(
                        "Same as the first check, but with slightly less absurd, yet often still extreme, differences.") },

                { "Minor",
                    new IssueTemplate(Issue.Level.Minor,
                        "{0} Space/time ratio is {1} times the expected, see e.g. {2}.",
                        "timestamp - ", "times", "example objects")
                    .WithCause(
                        "Same as the first check, but with more common differences.") }
            };
        }

        private readonly struct ObservedDistance
        {
            public readonly double deltaTime;
            public readonly double distance;
            public readonly HitObject hitObject;

            public ObservedDistance(double deltaTime, double distance, HitObject hitObject)
            {
                this.deltaTime = deltaTime;
                this.distance = distance;
                this.hitObject = hitObject;
            }
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            List<ObservedDistance> observedDistances = new List<ObservedDistance>();

            double ratioProblemThreshold = 15.0;
            double ratioWarningThreshold = 4.0;
            double ratioMinorThreshold   = 2.0;

            double snapLeniencyMs = 5;

            double deltaTime;
            double distance;

            foreach (var hitObject in beatmap.hitObjects)
            {
                HitObject nextObject = hitObject.Next();

                // Ignore spinners, since they have no clear start or end.
                if (hitObject is Spinner || nextObject is Spinner || nextObject == null)
                    continue;
                
                deltaTime = nextObject.GetPrevDeltaTime();
                // Ignore objects ~1/2 or more beats apart (assuming 160 bpm), since they're unlikely to be an issue.
                if (deltaTime > 180)
                    continue;

                distance = nextObject.GetPrevDistance();

                if (distance < 20)
                    distance = 20;

                List<ObservedDistance> sameSnappedDistances =
                    observedDistances
                        .FindAll(observedDistance =>
                            deltaTime <= observedDistance.deltaTime + snapLeniencyMs &&
                            deltaTime >= observedDistance.deltaTime - snapLeniencyMs &&
                            // Count the distances of sliders separately, as these have leniency unlike circles.
                            observedDistance.hitObject is Slider == hitObject is Slider);

                var observedDistance = new ObservedDistance(deltaTime, distance, hitObject);
                observedDistances.Add(observedDistance);

                if (!sameSnappedDistances.Any() ||
                    distance / deltaTime < sameSnappedDistances.Max(obvDist => obvDist.distance / obvDist.deltaTime * Decay(hitObject, obvDist)))
                {
                    continue;
                }

                if (distance <= beatmap.difficultySettings.GetCircleRadius() * 4)
                    continue;

                if (sameSnappedDistances.Count < 3)
                    // Too few samples, probably going to get inaccurate readings.
                    continue;

                double expectedDistance  = sameSnappedDistances.Sum(obvDist => obvDist.distance  * Decay(hitObject, obvDist)) / sameSnappedDistances.Count;
                double expectedDeltaTime = sameSnappedDistances.Sum(obvDist => obvDist.deltaTime * Decay(hitObject, obvDist)) / sameSnappedDistances.Count;

                if (hitObject is Slider)
                    // Account for slider follow circle leniency.
                    distance -= Math.Min(beatmap.difficultySettings.GetCircleRadius() * 3, distance);

                double actualExpectedRatio = (distance / deltaTime) / (expectedDistance / expectedDeltaTime);

                if (actualExpectedRatio <= ratioMinorThreshold)
                    continue;

                IEnumerable<string> comparisonTimestamps = sameSnappedDistances.Select(obvDist =>
                         Timestamp.Get(obvDist.hitObject, obvDist.hitObject.Next())
                    ).TakeLast(3);

                string templateName = "Minor";
                if (actualExpectedRatio > ratioProblemThreshold)
                    templateName = "Problem";
                else if (actualExpectedRatio > ratioWarningThreshold)
                    templateName = "Warning";

                yield return new Issue(GetTemplate(templateName), beatmap,
                    Timestamp.Get(hitObject, nextObject), Math.Round(actualExpectedRatio * 10) / 10,
                    string.Join("", comparisonTimestamps));
            }
        }

        private static double Decay(HitObject hitObject, ObservedDistance obvDist) =>
            Math.Min(1 / (hitObject.time - obvDist.hitObject.time) * 4000, 1);
    }
}
