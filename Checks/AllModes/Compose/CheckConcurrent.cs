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
                        "requires that the objects are in the same column.") },

                { "Almost Concurrent Objects",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} Within {1} ms of one another.",
                        "timestamp - ", "gap")
                    .WithCause(
                        "Two hit objects are less than 10 ms apart from one another. For mania this also " +
                        "requires that the objects are in the same column.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            int hitObjectCount = beatmap.hitObjects.Count();
            for (int i = 0; i < hitObjectCount - 1; ++i)
            {
                for (int j = i + 1; j < hitObjectCount; ++j)
                {
                    HitObject hitObject = beatmap.hitObjects[i];
                    HitObject otherHitObject = beatmap.hitObjects[j];

                    if (beatmap.generalSettings.mode == Beatmap.Mode.Mania &&
                        hitObject.Position.X != otherHitObject.Position.X)
                        continue;

                    // Only need to check forwards, as any previous object will already have looked behind this one.
                    double msApart = otherHitObject.time - hitObject.GetEndTime();

                    if (msApart <= 0)
                        yield return new Issue(GetTemplate("Concurrent Objects"), beatmap,
                            Timestamp.Get(hitObject, otherHitObject), ObjectsAsString(hitObject, otherHitObject));

                    else if (msApart <= 10)
                        yield return new Issue(GetTemplate("Almost Concurrent Objects"), beatmap,
                            Timestamp.Get(hitObject, otherHitObject), msApart);

                    else
                        // Hit objects are sorted by time, meaning if the next one is > 10 ms away, any remaining will be too.
                        break;
                }
            }
        }

        public string ObjectsAsString(HitObject hitObject, HitObject otherHitObject)
        {
            string type = hitObject.GetObjectType();
            string otherType = otherHitObject.GetObjectType();

            return
                type == otherType ?
                    type + "s" :
                    type + " and " + otherType;
        }
    }
}
