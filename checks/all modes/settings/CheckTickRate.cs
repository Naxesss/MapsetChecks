using MapsetChecks.objects;
using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.settings
{
    public class CheckTickRate : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Settings",
            Message = "Slider tick rates not aligning with any common beat snap divisor.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring that slider ticks align with the song's beat structure.
                    <image-right>
                        assets/docs/abnormalTickRate.jpg
                        A 1/1 slider with an asymmetric tick rate (neither a tick in the middle nor two equally distanced from it).
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Slider ticks, just like any other object, should align with the song in some way. If slider ticks are going after a 1/5 beat 
                    structure, for instance, that's either extremely rare or much more likely a mistake."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Tick Rate",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} {1}.",
                        "setting", "value")
                    .WithCause(
                        "The slider tick rate setting of a beatmap is using an incorrect or otherwise extremely uncommon divisor." +
                        "<note>Common tick rates include any full integer as well as 1/2, 4/3, and 3/2. Excludes precision errors.</note>") }
            };
        }
        
        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            // can't be decimal unless it's 0.5, 1.333 or 1.5 since they have practical use
            Issue issue = GetTickRateIssue(aBeatmap.difficultySettings.sliderTickRate, "slider tick rate", aBeatmap);
            if (issue != null)
                yield return issue;
        }

        /// <summary> Returns an issue when the given tick rate does not align with any integer value, 1/2, 3/2 or 4/3.
        /// Rounds the value to the closest 1/100th to avoid precision errors. </summary>
        private Issue GetTickRateIssue(float aTickRate, string aType, Beatmap aBeatmap)
        {
            double approxTickRate = Math.Round(aTickRate * 1000) / 1000;
            if (aTickRate - Math.Floor(aTickRate) != 0
                && approxTickRate != 0.5
                && approxTickRate != 1.333
                && approxTickRate != 1.5)
                return new Issue(GetTemplate("Tick Rate"), aBeatmap,
                    approxTickRate, aType);
            return null;
        }
    }
}
