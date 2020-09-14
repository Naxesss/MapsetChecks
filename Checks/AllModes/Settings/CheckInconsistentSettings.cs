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
    public class CheckInconsistentSettings : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Settings",
            Message = "Inconsistent mapset id, countdown, epilepsy warning, etc.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
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
            return new Dictionary<string, IssueTemplate>()
            {
                { "Problem",
                    new IssueTemplate(Issue.Level.Problem,
                        "Inconsistent {0}, see {1}.",
                        "setting", "difficulty")
                    .WithCause(
                        "The beatmapset id is inconsistent between any two difficulties in the set, regardless of mode.") },

                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "Inconsistent {0}, see {1}.",
                        "setting", "difficulty")
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
                        "Inconsistent {0}, see {1}.",
                        "setting", "difficulty")
                    .WithCause(
                        "Same as the warning, but only checks for slider ticks.") }
            };
        }
        
        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (Beatmap beatmap in beatmapSet.beatmaps)
            {
                foreach (Beatmap otherBeatmap in beatmapSet.beatmaps)
                {
                    if (beatmap.metadataSettings.beatmapSetId != otherBeatmap.metadataSettings.beatmapSetId)
                        yield return new Issue(GetTemplate("Problem"), beatmap,
                            "beatmapset id", otherBeatmap);

                
                    if (beatmap.generalSettings.mode == otherBeatmap.generalSettings.mode)
                    {
                        // Countdown has no effect in taiko or mania.
                        if (beatmap.generalSettings.mode != Beatmap.Mode.Taiko && beatmap.generalSettings.mode != Beatmap.Mode.Mania)
                        {
                            if (beatmap.GetCountdownStartBeat() >= 0 && otherBeatmap.GetCountdownStartBeat() >= 0)
                            {
                                if (beatmap.generalSettings.countdown != otherBeatmap.generalSettings.countdown)
                                    yield return new Issue(GetTemplate("Warning"), beatmap,
                                        "countdown speed", otherBeatmap);

                                if (beatmap.generalSettings.countdownBeatOffset != otherBeatmap.generalSettings.countdownBeatOffset)
                                    yield return new Issue(GetTemplate("Warning"), beatmap,
                                        "countdown offset", otherBeatmap);
                            }
                            else if (beatmap.GetCountdownStartBeat() >= 0 || otherBeatmap.GetCountdownStartBeat() >= 0)
                            {
                                if (beatmap.generalSettings.countdown > 0 || otherBeatmap.generalSettings.countdown > 0)
                                    yield return new Issue(GetTemplate("Warning"), beatmap,
                                        "countdown", otherBeatmap);
                            }
                        }

                        if (beatmap.breaks.Count > 0 && otherBeatmap.breaks.Count > 0)
                        {
                            if (beatmap.generalSettings.letterbox != otherBeatmap.generalSettings.letterbox)
                                yield return new Issue(GetTemplate("Warning"), beatmap,
                                    "letterbox", otherBeatmap);
                        }
                    }

                    // Widescreen support does nothing without a storyboard.
                    if (beatmap.HasDifficultySpecificStoryboard() &&
                        otherBeatmap.HasDifficultySpecificStoryboard() ||
                        beatmapSet.osb != null)
                    {
                        if (beatmap.generalSettings.widescreenSupport != otherBeatmap.generalSettings.widescreenSupport)
                            yield return new Issue(GetTemplate("Warning"), beatmap,
                                "widescreen support", otherBeatmap);

                        if (beatmap.generalSettings.storyInFrontOfFire != otherBeatmap.generalSettings.storyInFrontOfFire)
                            yield return new Issue(GetTemplate("Warning"), beatmap,
                                "storyboard in front of combo fire", otherBeatmap);

                        if (beatmap.generalSettings.useSkinSprites != otherBeatmap.generalSettings.useSkinSprites)
                            yield return new Issue(GetTemplate("Warning"), beatmap,
                                "usage of skin sprites in storyboard", otherBeatmap);
                    }

                    // Only warn on the difficulty with the storyboard.
                    if (beatmap.HasDifficultySpecificStoryboard() && !otherBeatmap.HasDifficultySpecificStoryboard())
                        yield return new Issue(GetTemplate("Warning"), beatmap,
                            "difficulty-specific storyboard presence", otherBeatmap);

                    // Epilepsy warning requires either a storyboard or video to show.
                    if (beatmap.HasDifficultySpecificStoryboard() &&
                        otherBeatmap.HasDifficultySpecificStoryboard() ||
                        beatmapSet.osb != null ||
                        beatmap.videos.Count > 0 && otherBeatmap.videos.Count > 0)
                    {
                        if (beatmap.generalSettings.epilepsyWarning != otherBeatmap.generalSettings.epilepsyWarning)
                            yield return new Issue(GetTemplate("Warning"), beatmap,
                                "epilepsy warning", otherBeatmap);
                    }

                    if (beatmap.generalSettings.audioLeadIn != otherBeatmap.generalSettings.audioLeadIn)
                        yield return new Issue(GetTemplate("Warning"), beatmap,
                            "audio lead-in", otherBeatmap);

                    if (beatmap.generalSettings.skinPreference != otherBeatmap.generalSettings.skinPreference)
                        yield return new Issue(GetTemplate("Warning"), beatmap,
                            "skin preference", otherBeatmap);

                    if (beatmap.difficultySettings.sliderTickRate != otherBeatmap.difficultySettings.sliderTickRate)
                        yield return new Issue(GetTemplate("Minor"), beatmap,
                            "slider tick rate", otherBeatmap);
                }
            }
        }
    }
}
