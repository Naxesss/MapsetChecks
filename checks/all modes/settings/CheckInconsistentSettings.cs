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
                    more modern and exclude this.
                    <note>
                        The Ranking Criteria currently states that inconsistent letterboxing within the same mode is disallowed under 
                        any circumstance, but this is simply because no one has tried ranking a beatmap with a letterbox as one of its 
                        thematic features. Once someone attempts and succeeds at this, the rule will very likely be changed to account 
                        for it.
                    </note>"
                },
                {
                    "Specifics",
                    @"
                    The following settings are checked for and are assigned their respective issue level if inconsistent between 
                    difficulties of the same mode (excluding beatmapset id): 
                    <div style=""margin:8px 16px;"">
                        <div class=""card-detail-icon cross-icon""></div>beatmapset id
                        <br \><div class=""card-detail-icon exclamation-icon""></div>countdown speed (if there's enough time to show it, excluded for taiko/mania)
                        <br \><div class=""card-detail-icon exclamation-icon""></div>countdown offset (if there's enough time to show it, excluded for taiko/mania)
                        <br \><div class=""card-detail-icon exclamation-icon""></div>countdown (if there's enough time to show it, excluded for taiko/mania)
                        <br \><div class=""card-detail-icon exclamation-icon""></div>letterbox (if there are breaks)
                        <br \><div class=""card-detail-icon exclamation-icon""></div>widescreen support (if there's an sb)
                        <br \><div class=""card-detail-icon exclamation-icon""></div>storyboard presence
                        <br \><div class=""card-detail-icon exclamation-icon""></div>epilepsy warning (if there's an sb or video)
                        <br \><div class=""card-detail-icon exclamation-icon""></div>audio lead-in
                        <br \><div class=""card-detail-icon exclamation-icon""></div>skin preference
                        <br \><div class=""card-detail-icon exclamation-icon""></div>storyboard in front of combo fire (if there's a storyboard)
                        <br \><div class=""card-detail-icon exclamation-icon""></div>usage of skin sprites in storyboard (if there's a storyboard)
                        <br \><div class=""card-detail-icon minor-icon""></div>slider tick rate
                    </div>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Unrankable",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "Inconsistent {0}, see {1}.",
                        "setting", "difficulty")
                    .WithCause(
                        "One of the settings checked for in a beatmap was different from the reference beatmap.") },

                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "Inconsistent {0}, see {1}.",
                        "setting", "difficulty")
                    .WithCause(
                        "Same as the other checks, but not necessarily required to be the same.") },

                { "Minor",
                    new IssueTemplate(Issue.Level.Minor,
                        "Inconsistent {0}, see {1}.",
                        "setting", "difficulty")
                    .WithCause(
                        "Same as the other checks, but commonly different.") }
            };
        }
        
        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
            {
                foreach (Beatmap otherBeatmap in aBeatmapSet.beatmaps)
                {
                    if (beatmap.metadataSettings.beatmapSetId != otherBeatmap.metadataSettings.beatmapSetId)
                        yield return new Issue(GetTemplate("Unrankable"), beatmap,
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
                        aBeatmapSet.osb != null)
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
                    else if (
                        aBeatmapSet.osb == null &&
                        (beatmap.HasDifficultySpecificStoryboard() ||
                        otherBeatmap.HasDifficultySpecificStoryboard()))
                    {
                        yield return new Issue(GetTemplate("Warning"), beatmap,
                            "storyboard", otherBeatmap);
                    }

                    // Epilepsy warning requires either a storyboard or video to show.
                    if (beatmap.HasDifficultySpecificStoryboard() &&
                        otherBeatmap.HasDifficultySpecificStoryboard() ||
                        aBeatmapSet.osb != null ||
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
