using MapsetParser.objects;
using MapsetParser.statics;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MapsetChecks.checks.timing
{
    [Check]
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
                    <div style=""margin:16px;"">
                        ""At overall difficulty 10, this would result in at most 2.4% chance of a 300 becoming a 100. It would change the 
                        ratio of 300s to 100s by 1.6%. This is acceptable, and a much larger variable is introduced by the possibility of 
                        lag, input latency and time calculation accuracy.""
                        <div style=""margin:8px 0px;"">
                            — peppy, 2009 in <a href=""https://osu.ppy.sh/community/forums/topics/17711"">Ctrl+V rounding error [Resolved]</a>
                        </div>
                    </div>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Problem",
                    new IssueTemplate(Issue.Level.Problem,
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
            foreach (HitObject hitObject in beatmap.hitObjects)
                foreach (double edgeTime in hitObject.GetEdgeTimes())
                    foreach (Issue issue in GetUnsnapIssue(hitObject.GetPartName(edgeTime), edgeTime, beatmap))
                        yield return issue;
        }

        /// <summary> Returns issues wherever the given time value is unsnapped. </summary>
        private IEnumerable<Issue> GetUnsnapIssue(string type, double time, Beatmap beatmap)
        {
            double? unsnapIssue = beatmap.GetUnsnapIssue(time);
            double unsnap = beatmap.GetPracticalUnsnap(time);

            if (unsnapIssue != null)
                yield return new Issue(GetTemplate("Problem"), beatmap,
                    Timestamp.Get(time), type, $"{unsnap:0.###}");

            else if (Math.Abs(unsnap) >= 1)
                yield return new Issue(GetTemplate("Minor"), beatmap,
                    Timestamp.Get(time), type, $"{unsnap:0.###}");
        }
    }
}
