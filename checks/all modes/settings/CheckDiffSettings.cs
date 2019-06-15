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
                    minimum and maximum values, 2 and 7 respectively (1 and 9 for mania). These limits intentional and represent the 
                    largest and smallest circle size acceptable.
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
                { "CS",
                    new IssueTemplate(Issue.Level.Problem,
                        "Circle Size {0} is less than {1} or greater than {2}.",
                        "setting", "min", "max")
                    .WithCause(
                        "Circle size is less than 2 (1 for mania) or greater than 7 (9 for mania). " +
                        "Ignored in taiko.") },

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

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            Issue issue = GetIssue(aBeatmap.difficultySettings.hpDrain, "HP Drain Rate", aBeatmap);
            if (issue != null)
                yield return issue;

            // Circle size does nothing in taiko.
            if (aBeatmap.generalSettings.mode != Beatmap.Mode.Taiko)
            {
                issue = GetIssue(aBeatmap.difficultySettings.circleSize, "Circle Size", aBeatmap,
                    aBeatmap.generalSettings.mode == Beatmap.Mode.Mania ? 1 : 2,
                    aBeatmap.generalSettings.mode == Beatmap.Mode.Mania ? 9 : 7);
                if (issue != null)
                    yield return issue;
            }

            issue = GetIssue(aBeatmap.difficultySettings.approachRate, "Approach Rate", aBeatmap);
            if (issue != null)
                yield return issue;

            issue = GetIssue(aBeatmap.difficultySettings.overallDifficulty, "Overall Difficulty", aBeatmap);
            if (issue != null)
                yield return issue;
        }

        /// <summary> Returns an issue when a setting is either less than the minimum, more than the maximum or
        /// contains more than 1 decimal place. </summary>
        private Issue GetIssue(float aDifficulty, string aType, Beatmap aBeatmap, int aMin = 0, int aMax = 10)
        {
            if (aDifficulty < aMin ||
                aDifficulty > aMax)
            {
                if (aType == "Circle Size")
                    return new Issue(GetTemplate("CS"), aBeatmap,
                        $"{aDifficulty:0.####}", aMin, aMax);
                else
                    return new Issue(GetTemplate("Other"), aBeatmap,
                        $"{aDifficulty:0.####}", aType);
            }
            else if (aDifficulty - (float)Math.Floor(aDifficulty * 10) / 10 > 0)
            {
                return new Issue(GetTemplate("Decimals"), aBeatmap,
                    $"{aDifficulty:0.####}", aType);
            }

            return null;
        }
    }
}
