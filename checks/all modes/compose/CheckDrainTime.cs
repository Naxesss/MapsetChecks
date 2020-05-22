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
    public class CheckDrainTime : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Compose",
            Message = "Too short drain time.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Prevents beatmaps from being too short, for example 10 seconds long.
                    <image>
                        https://i.imgur.com/uNDPeJI.png
                        A beatmap with a total mp3 length of ~21 seconds.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Beatmaps this short do not offer a substantial enough gameplay experience for the standards of 
                    the ranked section."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Problem",
                    new IssueTemplate(Issue.Level.Problem,
                        "Less than 30 seconds of drain time, currently {0}.",
                        "drain time")
                    .WithCause(
                        "The time from the first object to the end of the last object, subtracting any time between two objects " +
                        "where a break exists, is in total less than 30 seconds.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            if (beatmap.GetDrainTime() >= 30 * 1000)
                yield break;

            yield return new Issue(GetTemplate("Problem"), beatmap,
                Timestamp.Get(beatmap.GetDrainTime()));
        }
    }
}
