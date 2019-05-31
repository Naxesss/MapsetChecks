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
            Author = "Naxess",
            
            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Prevent hit objects from being unsnapped by more than 1 ms."
                },
                {
                    "Reasoning",
                    @"
                    Since gameplay is based on audio cues it wouldn't make much sense to have certain hit windows happen earlier or later, 
                    even if only by a few ms.
                    <br \><br \>
                    The only reason a 1 ms leniency exists is because the editor casts decimal times to integers rather than rounds them 
                    properly, which causes these 1 ms unsnaps occasionally when copying and pasting hit objects and timing lines. This bug 
                    happens so frequently that basically all ranked maps have multiple 1 ms unsnaps in them.
                    <note>
                        When asked about this a long time ago, peppy responded with that 1 ms unsnaps are not noticable, and 
                        proceeded with adding a 1 ms leniency to AiMod to prevent them from being detected.
                    </note>"
                }
            }
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
                foreach (double edgeTime in hitObject.GetEdgeTimes())
                    foreach (Issue issue in GetUnsnapIssue(hitObject.GetPartName(edgeTime), edgeTime, aBeatmap))
                        yield return issue;
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
