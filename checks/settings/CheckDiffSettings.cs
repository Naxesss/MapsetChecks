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
            Message = "Illegal difficulty settings.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "CS",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "Circle Size {0} is less than {1} or greater than {2}.",
                        "setting", "min", "max")
                    .WithCause(
                        "Circle size is less than 2 (1 for mania) or greater than 7 (9 for mania).") },

                { "Decimals",
                    new IssueTemplate(Issue.Level.Unrankable,
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
                        (Math.Round(aDifficulty * 1000) / 1000).ToString(CultureInfo.InvariantCulture), aMin, aMax);
                else
                    return new Issue(GetTemplate("Other"), aBeatmap,
                        (Math.Round(aDifficulty * 1000) / 1000).ToString(CultureInfo.InvariantCulture), aType);
            }
            else if (aDifficulty - (float)Math.Floor(aDifficulty * 10) / 10 > 0)
            {
                return new Issue(GetTemplate("Decimals"), aBeatmap,
                    (Math.Round(aDifficulty * 1000) / 1000).ToString(CultureInfo.InvariantCulture), aType);
            }

            return null;
        }
    }
}
