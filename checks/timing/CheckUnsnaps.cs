using MapsetParser.objects;
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

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (HitObject myHitObject in beatmap.hitObjects)
            {
                string myType =
                    myHitObject.HasType(HitObject.Type.Circle) ? "Circle" :
                    myHitObject.HasType(HitObject.Type.Slider) ? "Slider" :
                    "Spinner";

                foreach (double myEdgeTime in myHitObject.GetEdgeTimes())
                    foreach (Issue myIssue in GetIssue(
                            myEdgeTime == myHitObject.time
                                ? myType + (myType == "Circle"
                                    ? "" : " head")
                                : myType + (myEdgeTime == myHitObject.GetEndTime()
                                    ? " tail" : " repeat"),
                            myEdgeTime, beatmap, myHitObject))
                        yield return myIssue;
            }
        }

        private IEnumerable<Issue> GetIssue<T>(string aType, double aTime, Beatmap aBeatmap, params T[] anObject)
        {
            double? myUnsnapIssue = aBeatmap.GetUnsnapIssue(aTime);

            double myUnsnap = aBeatmap.GetPracticalUnsnap(aTime);
            double myRoundedUnsnap = Math.Round(myUnsnap * 1000) / 1000;
            string myUnsnapString = myRoundedUnsnap.ToString(CultureInfo.InvariantCulture);

            if (myUnsnapIssue != null)
                yield return new Issue(GetTemplate("Unrankable"), aBeatmap,
                    Timestamp.Get(aTime), aType, myUnsnapString);

            else if (Math.Abs(myUnsnap) >= 1)
                yield return new Issue(GetTemplate("Minor"), aBeatmap,
                    Timestamp.Get(aTime), aType, myUnsnapString);
        }
    }
}
