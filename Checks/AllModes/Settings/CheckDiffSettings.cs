﻿using System;
using System.Collections.Generic;
using MapsetParser.objects;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;

namespace MapsetChecks.Checks.AllModes.Settings
{
    [Check]
    public class CheckDiffSettings : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Category = "Settings",
            Message = "Abnormal difficulty settings.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Preventing difficulty settings from including more than 1 decimal, and mania key counts from being too large or small."
                },
                {
                    "Reasoning",
                    @"
                    Settings having more than 1 decimal place is currently unrankable for, what is probably, two reasons:
                    <ul>
                        <li>
                            The precision for anything greater than 1 place matters too little to be worth increasing the amount of 
                            digits in the song selection screen/website.
                        </li>
                        <li>
                            Searching for e.g. AR=9.25 will not find maps with AR 9.25. However, searching for AR=9.2 will.
                        </li>
                    </ul>
                    <image>
                        https://i.imgur.com/ySldNaU.png
                        More than 1 decimal place compared to 1 decimal place.
                    </image>

                    The circle size setting in mania determines the key count, and is limited between 4 and 18.
                    <ul>
                        <li>
                            3K or fewer leaves little room for patterning, making 2 maps of the same difficulty and song almost identical.
                        </li>
                    </ul>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "CS Mania",
                    new IssueTemplate(Issue.Level.Problem,
                        "Key count {0} is less than {1} or greater than {2}.",
                        "setting", "min", "max")
                    .WithCause(
                        "The circle size settings is less than 4 or greater than 18. Only applies to mania.") },

                { "Decimals",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} {1} has more than 1 decimal place.",
                        "value", "setting")
                    .WithCause(
                        "A difficulty setting has more than 1 decimal place.") },

                { "Other",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} {1}, although is capped between 0 to 10 in-game.",
                        "value", "setting")
                    .WithCause(
                        "A difficulty setting is less than 0 or greater than 10.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var issue = GetIssue(beatmap.difficultySettings.hpDrain, "HP Drain Rate", beatmap);
            if (issue != null)
                yield return issue;

            // Circle size does nothing in taiko.
            if (beatmap.generalSettings.mode == Beatmap.Mode.Mania)
            {
                issue = GetIssue(beatmap.difficultySettings.circleSize, "Circle Size", beatmap, minSetting: 4, maxSetting: 18);
                if (issue != null)
                    yield return issue;
            }
            else
            {
                issue = GetIssue(beatmap.difficultySettings.approachRate, "Circle Size", beatmap);
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
                if (type == "Circle Size" && beatmap.generalSettings.mode == Beatmap.Mode.Mania)
                    return new Issue(GetTemplate("CS Mania"), beatmap,
                        $"{setting:0.####}", minSetting, maxSetting);
                
                return new Issue(GetTemplate("Other"), beatmap,
                    $"{setting:0.####}", type);
            }

            if (setting - (float)Math.Floor(setting * 10) / 10 > 0)
                return new Issue(GetTemplate("Decimals"), beatmap,
                    $"{setting:0.####}", type);

            return null;
        }
    }
}
