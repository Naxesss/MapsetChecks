using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.compose
{
    [Check]
    public class CheckConcurrent : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Compose",
            Message = "Concurrent hit objects.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring that only one object needs to be interacted with at any given moment in time."
                },
                {
                    "Reasoning",
                    @"
                    A clickable object during the duration of an already clicked object, for example a slider, is possible to play 
                    assuming the clickable object is within the slider circle whenever a slider tick/edge happens. However, there is 
                    no way for a player to intuitively know how to play such patterns as there is no tutorial for them, and they are 
                    not self-explanatory.
                    <br \><br \>
                    Sliders, spinners, and other holdable objects, teach the player to hold down the key for 
                    the whole duration of the object, so suddenly forcing them to press again would be contradictory to that 
                    fundamental understanding. Because of this, these patterns more often than not cause confusion, even where 
                    otherwise introduced well.
                    <image-right>
                        https://i.imgur.com/2bTX4aQ.png
                        A slider with two concurrent circles. Can be hit without breaking combo.
                    </image>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Concurrent Objects",
                    new IssueTemplate(Issue.Level.Problem,
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
                            Timestamp.Get(hitObject, otherHitObject), argument);
                    }
                }
            }
        }
    }
}
