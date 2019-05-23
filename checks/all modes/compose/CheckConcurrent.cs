using MapsetChecks.objects;
using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.compose
{
    public class CheckConcurrent : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Compose",
            Message = "Concurrent hit objects.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Concurrent Objects",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} Concurrent {1}.",
                        "timestamp - ", "hit objects")
                    .WithCause(
                        "A hit object starts before another hit object has ended. For mania this also " +
                        "requires that the objects are in the same column.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            List<HitObject> iteratedObjects = new List<HitObject>();
            foreach (HitObject hitObject in aBeatmap.hitObjects)
            {
                iteratedObjects.Add(hitObject);
                foreach (HitObject otherHitObject in aBeatmap.hitObjects.Except(iteratedObjects))
                {
                    bool sameTimeline =
                        aBeatmap.generalSettings.mode != Beatmap.Mode.Mania ||
                        hitObject.Position.X == otherHitObject.Position.X;

                    bool concurrent =
                        otherHitObject.time <= hitObject.GetEndTime() &&
                        otherHitObject.time >= hitObject.time;

                    if (concurrent && sameTimeline)
                    {
                        string type      = hitObject.GetObjectType();
                        string otherType = otherHitObject.GetObjectType();

                        string argument =
                            type == otherType ?
                                type + "s" :
                                type + " and " + otherType;

                        yield return new Issue(GetTemplate("Concurrent Objects"), aBeatmap,
                            Timestamp.Get(otherHitObject), argument);
                    }
                }
            }
        }
    }
}
