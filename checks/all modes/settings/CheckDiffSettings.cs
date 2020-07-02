using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.settings
{
    [Check]
    public class CheckDiffSettings : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Settings",
            Message = "Abnormal difficulty settings.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing circle size from being too small or large and difficulty settings from including more than 1 decimal."
                },
                {
                    "Reasoning",
                    @"
                    All difficulty settings cap at 0 and 10, including circle size, making it the only setting that can go beyond its 
                    minimum and maximum values, 2 and 7 respectively (1 and 9 for mania). These limits are intentional and represent the 
                    largest and smallest circle size acceptable, anything smaller or larger is considered ridiculous.
                    <image>
                        https://i.imgur.com/JT9JNMb.jpg
                        Circle size 0 compared to circle size 10.
                    </image>

                    Settings should also not have more than 1 decimal place, as the precision for anything greater matters too little 
                    to be worth increasing the amount of digits in the song selection screen or website for, even if it already rounds 
                    to 2 decimal places in-game.
                    <image>
                        https://i.imgur.com/ySldNaU.png
                        More than 1 decimal place compared to 1 decimal place.
                    </image>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "CS Mania",
                    new IssueTemplate(Issue.Level.Problem,
                        "Key count {0} is less than {1} or greater than {2}.",
                        "setting", "min", "max")
                    .WithCause(
                        "The circle size settings is less than 4 or greater than 9. Only applies to mania.") },

                { "Decimals",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} {1} has more than 1 decimal place.",
                        "value", "setting")
                    .WithCause(
                        "A difficulty setting has more than 1 decimal place.") },

                { "Other",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} {1}, although is normalized to 0 to 10 in-game.",
                        "value", "setting")
                    .WithCause(
                        "A difficulty setting (not CS) is less than 0 or greater than 10.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            Issue issue = GetIssue(beatmap.difficultySettings.hpDrain, "HP Drain Rate", beatmap);
            if (issue != null)
                yield return issue;

            // Circle size does nothing in taiko.
            if (beatmap.generalSettings.mode == Beatmap.Mode.Mania)
            {
                issue = GetIssue(beatmap.difficultySettings.circleSize, "Circle Size", beatmap, minSetting: 4, maxSetting: 9);
                if (issue != null)
                    yield return issue;
            }

            issue = GetIssue(beatmap.difficultySettings.approachRate, "Approach Rate", beatmap);
            if (issue != null)
                yield return issue;

            issue = GetIssue(beatmap.difficultySettings.overallDifficulty, "Overall Difficulty", beatmap);
            if (issue != null)
                yield return issue;
        }

        /// <summary> Returns an issue when a setting is either less than the minimum, more than the maximum or
        /// contains more than 1 decimal place. </summary>
        private Issue GetIssue(float setting, string type, Beatmap beatmap, int minSetting = 0, int maxSetting = 10)
        {
            if (setting < minSetting ||
                setting > maxSetting)
            {
                if (type == "Circle Size")
                    return new Issue(GetTemplate("CS Mania"), beatmap,
                        $"{setting:0.####}", minSetting, maxSetting);
                else
                    return new Issue(GetTemplate("Other"), beatmap,
                        $"{setting:0.####}", type);
            }
            else if (setting - (float)Math.Floor(setting * 10) / 10 > 0)
            {
                return new Issue(GetTemplate("Decimals"), beatmap,
                    $"{setting:0.####}", type);
            }

            return null;
        }
    }
}
