using MapsetParser.objects;
using MapsetParser.statics;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MapsetChecks.checks.timing
{
    public class CheckUnsnaps : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Timing",
            Message = "Unsnapped hit objects.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Unrankable",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} {1} unsnapped by {2} ms.",
                        "timestamp - ", "object", "unsnap")
                    .WithCause(
                        "A hit object is snapped at least 2 ms too early or late for either of the 1/12 or 1/16 beat snap divisors.") },

                { "Minor",
                    new IssueTemplate(Issue.Level.Minor,
                        "{0} {1} unsnapped by {2} ms.",
                        "timestamp - ", "object", "unsnap")
                    .WithCause(
                        "Same as the other check, but by 1 ms or more instead.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            foreach (HitObject hitObject in aBeatmap.hitObjects)
            {
                string objectType =
                    hitObject.type.HasFlag(HitObject.Type.Circle) ? "Circle" :
                    hitObject.type.HasFlag(HitObject.Type.Slider) ? "Slider" :
                    "Spinner";

                foreach (double edgeTime in hitObject.GetEdgeTimes())
                    foreach (Issue issue in GetUnsnapIssue(
                            edgeTime == hitObject.time
                                ? objectType + (objectType == "Circle"
                                    ? "" : " head")
                                : objectType + (edgeTime == hitObject.GetEndTime()
                                    ? " tail" : " repeat"),
                            edgeTime, aBeatmap))
                        yield return issue;
            }
        }

        /// <summary> Returns issues wherever the given time value is unsnapped. </summary>
        private IEnumerable<Issue> GetUnsnapIssue(string aType, double aTime, Beatmap aBeatmap)
        {
            double? unsnapIssue = aBeatmap.GetUnsnapIssue(aTime);

            double unsnap = aBeatmap.GetPracticalUnsnap(aTime);
            double roundedUnsnap = Math.Round(unsnap * 1000) / 1000;
            string unsnapString = roundedUnsnap.ToString(CultureInfo.InvariantCulture);

            if (unsnapIssue != null)
                yield return new Issue(GetTemplate("Unrankable"), aBeatmap,
                    Timestamp.Get(aTime), aType, unsnapString);

            else if (Math.Abs(unsnap) >= 1)
                yield return new Issue(GetTemplate("Minor"), aBeatmap,
                    Timestamp.Get(aTime), aType, unsnapString);
        }
    }
}
