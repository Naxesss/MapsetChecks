using System;
using System.Collections.Generic;
using MapsetParser.objects;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;

namespace MapsetChecks.Checks.AllModes.Settings
{
    [Check]
    public class CheckInconsistentSettings : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Category = "Settings",
            Message = "Inconsistent mapset id, countdown, epilepsy warning, etc.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Ensuring settings across difficulties in a beatmapset are consistent within game modes and where it makes sense."
                },
                {
                    "Reasoning",
                    @"
                    Difficulties in a beatmapset using a similar video or storyboard, would likely want to have the same 
                    epilepsy settings since they would share the same reason to have it. Same goes for countdown, letterboxing, 
                    widescreen support, audio lead-in, etc. Obviously excluding settings that don't apply.
                    <br \><br \>
                    Having difficulties all be different in terms of noticeable settings would make the set seem less coherent, 
                    but it can be acceptable if it differs thematically in a way that makes it seem intentional, without needing to 
                    specify that it is, for example one beatmap being old-style with a special skin and countdown while others are 
                    more modern and exclude this."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "Problem",
                    new IssueTemplate(Issue.Level.Problem,
                        "Inconsistent {0} \"{1}\", see {2} \"{3}\".",
                        "setting", "value", "difficulty", "value")
                    .WithCause(
                        "The beatmapset id is inconsistent between any two difficulties in the set, regardless of mode.") },

                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "Inconsistent {0} \"{1}\", see {2} \"{3}\".",
                        "setting", "value", "difficulty", "value")
                    .WithCause(
                        @"Compares settings and presence of elements within the same mode. Includes the following:
                        <ul>
                            <li>countdown speed (if there's enough time to show it, excluded for taiko/mania)</li>
                            <li>countdown offset (if there's enough time to show it, excluded for taiko/mania)</li>
                            <li>countdown presence (if there's enough time to show it, excluded for taiko/mania)</li>
                            <li>letterbox (if there are breaks)</li>
                            <li>widescreen support (if there's a storyboard)</li>
                            <li>difficulty-specific storyboard presence</li>
                            <li>epilepsy warning (if there's a storyboard or video)</li>
                            <li>audio lead-in</li>
                            <li>skin preference</li>
                            <li>storyboard in front of combo fire (if there's a storyboard)</li>
                            <li>usage of skin sprites in storyboard (if there's a storyboard)</li>
                        </ul>
                        <note>
                            Inconsistent video is already covered by another check.
                        </note>") },

                { "Minor",
                    new IssueTemplate(Issue.Level.Minor,
                        "Inconsistent {0} \"{1}\", see {2} \"{3}\".",
                        "setting", "value", "difficulty", "value")
                    .WithCause(
                        "Same as the warning, but only checks for slider ticks.") }
            };
        }

        private static readonly Func<Beatmap, Beatmap, BeatmapSet, bool> CountdownSettingCondition =
            (beatmap, otherBeatmap, beatmapSet) =>
                beatmap.generalSettings.mode == otherBeatmap.generalSettings.mode &&
                // Countdown has no effect in taiko or mania.
                beatmap.generalSettings.mode != Beatmap.Mode.Taiko &&
                beatmap.generalSettings.mode != Beatmap.Mode.Mania;

        private static readonly Func<Beatmap, Beatmap, BeatmapSet, bool> StoryboardCondition =
            (beatmap, otherBeatmap, beatmapSet) =>
                beatmap.HasDifficultySpecificStoryboard() &&
                otherBeatmap.HasDifficultySpecificStoryboard() ||
                beatmapSet.osb != null;

        private readonly struct InconsistencyTemplate
        {
            public readonly string template;
            public readonly string name;
            public readonly Func<Beatmap, object> value;
            public readonly Func<Beatmap, Beatmap, BeatmapSet, bool> condition;

            public InconsistencyTemplate(string template,
                                         string name,
                                         Func<Beatmap, object> value,
                                         Func<Beatmap, Beatmap, BeatmapSet, bool> condition = null)
            {
                this.template = template;
                this.name = name;
                this.value = value;
                this.condition = condition;
            }
        }

        private static readonly List<InconsistencyTemplate> InconsistencyTemplates = new List<InconsistencyTemplate>
        {
            new InconsistencyTemplate(
                template: "Problem",
                name:     "beatmapset id",
                value:    beatmap =>
                    beatmap.metadataSettings.beatmapSetId != null
                        ? beatmap.metadataSettings.beatmapSetId.ToString()
                        : "-1"  // Beatmapset IDs are set to -1 for unsubmitted mapsets.
            ),
            new InconsistencyTemplate(
                template:  "Warning",
                name:      "countdown speed",
                value:     beatmap => beatmap.generalSettings.countdown,
                condition: (beatmap, otherBeatmap, beatmapSet) =>
                    CountdownSettingCondition(beatmap, otherBeatmap, beatmapSet) &&
                    // CountdownStartBeat < 0 means no countdown.
                    beatmap.GetCountdownStartBeat() >= 0 &&
                    otherBeatmap.GetCountdownStartBeat() >= 0
            ),
            new InconsistencyTemplate(
                template:  "Warning",
                name:      "countdown offset",
                value:     beatmap => beatmap.generalSettings.countdownBeatOffset,
                condition: (beatmap, otherBeatmap, beatmapSet) =>
                    CountdownSettingCondition(beatmap, otherBeatmap, beatmapSet) &&
                    beatmap.GetCountdownStartBeat() >= 0 &&
                    otherBeatmap.GetCountdownStartBeat() >= 0
            ),
            new InconsistencyTemplate(
                template:  "Warning",
                name:      "countdown",
                value:     beatmap => beatmap.generalSettings.countdown,
                condition: (beatmap, otherBeatmap, beatmapSet) =>
                    CountdownSettingCondition(beatmap, otherBeatmap, beatmapSet) &&
                    // One map has countdown, the other not.
                    beatmap.GetCountdownStartBeat() >= 0 != otherBeatmap.GetCountdownStartBeat() >= 0
            ),
            new InconsistencyTemplate(
                template:  "Warning",
                name:      "letterbox",
                value:     beatmap => beatmap.generalSettings.letterbox,
                condition: (beatmap, otherBeatmap, beatmapSet) =>
                    beatmap.generalSettings.mode == otherBeatmap.generalSettings.mode &&
                    beatmap.breaks.Count > 0 && otherBeatmap.breaks.Count > 0
            ),
            new InconsistencyTemplate(
                template:  "Warning",
                name:      "widescreen support",
                value:     beatmap => beatmap.generalSettings.widescreenSupport,
                condition: StoryboardCondition
            ),
            new InconsistencyTemplate(
                template:  "Warning",
                name:      "storyboard in front of combo fire",
                value:     beatmap => beatmap.generalSettings.storyInFrontOfFire,
                condition: StoryboardCondition
            ),
            new InconsistencyTemplate(
                template:  "Warning",
                name:      "usage of skin sprites in storyboard",
                value:     beatmap => beatmap.generalSettings.useSkinSprites,
                condition: StoryboardCondition
            ),
            new InconsistencyTemplate(
                template:  "Warning",
                name:      "difficulty-specific storyboard presence",
                value:     beatmap => beatmap.HasDifficultySpecificStoryboard()
            ),
            new InconsistencyTemplate(
                template:  "Warning",
                name:      "epilepsy warning",
                value:     beatmap => beatmap.generalSettings.epilepsyWarning,
                condition: (beatmap, otherBeatmap, beatmapSet) =>
                    StoryboardCondition(beatmap, otherBeatmap, beatmapSet) ||
                    beatmap.videos.Count > 0 && otherBeatmap.videos.Count > 0
            ),
            new InconsistencyTemplate(
                template:  "Warning",
                name:      "audio lead-in",
                value:     beatmap => beatmap.generalSettings.audioLeadIn
            ),
            new InconsistencyTemplate(
                template:  "Warning",
                name:      "skin preference",
                value:     beatmap => beatmap.generalSettings.skinPreference
            ),
            new InconsistencyTemplate(
                template:  "Minor",
                name:      "slider tick rate",
                value:     beatmap => beatmap.difficultySettings.sliderTickRate,
                condition: (beatmap, otherBeatmap, beatmapSet) =>
                    beatmap.generalSettings.mode == otherBeatmap.generalSettings.mode
            )
        };
        
        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var beatmap in beatmapSet.beatmaps)
                foreach (var otherBeatmap in beatmapSet.beatmaps)
                    foreach (var inconsistency in InconsistencyTemplates)
                        // `GetInconsistency` returns either 1 or 0 issues, so this becomes O(n^2*m),
                        // where n is amount of beatmaps and m is amount of inconsistencies checked.
                        foreach (Issue issue in GetInconsistency(beatmap, otherBeatmap, beatmapSet, inconsistency))
                            yield return issue;
        }

        private IEnumerable<Issue> GetInconsistency(Beatmap               beatmap,
                                                    Beatmap               otherBeatmap,
                                                    BeatmapSet            beatmapSet,
                                                    InconsistencyTemplate inconsistency)
        {
            if (inconsistency.condition != null && !inconsistency.condition(beatmap, otherBeatmap, beatmapSet))
                yield break;
            
            string value = inconsistency.value(beatmap).ToString();
            string otherValue = inconsistency.value(otherBeatmap).ToString();
            if (value != otherValue)
                yield return new Issue(GetTemplate(inconsistency.template), beatmap,
                    inconsistency.name, value, otherBeatmap, otherValue);
        }
    }
}
