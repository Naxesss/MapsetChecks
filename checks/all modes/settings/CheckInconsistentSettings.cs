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
            Author = "Naxess"
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
            Beatmap refBeatmap = aBeatmapSet.beatmaps.First();
            string version = refBeatmap.metadataSettings.version;

            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
            {
                if (beatmap.metadataSettings.beatmapSetId != refBeatmap.metadataSettings.beatmapSetId)
                    yield return new Issue(GetTemplate("Unrankable"), beatmap,
                        "beatmapset id", refBeatmap);

                // Countdown has no effect in taiko or mania.
                if (beatmap.generalSettings.mode != Beatmap.Mode.Taiko && beatmap.generalSettings.mode != Beatmap.Mode.Mania)
                {
                    if (beatmap.GetCountdownStartBeat() >= 0 && refBeatmap.GetCountdownStartBeat() >= 0)
                    {
                        if (beatmap.generalSettings.countdown != refBeatmap.generalSettings.countdown)
                            yield return new Issue(GetTemplate("Warning"), beatmap,
                                "countdown speed", refBeatmap);

                        if (beatmap.generalSettings.countdownBeatOffset != refBeatmap.generalSettings.countdownBeatOffset)
                            yield return new Issue(GetTemplate("Warning"), beatmap,
                                "countdown offset", refBeatmap);
                    }
                    else if (beatmap.GetCountdownStartBeat() >= 0 || refBeatmap.GetCountdownStartBeat() >= 0)
                    {
                        if (beatmap.generalSettings.countdown > 0 || refBeatmap.generalSettings.countdown > 0)
                            yield return new Issue(GetTemplate("Warning"), beatmap,
                                "countdown", refBeatmap);
                    }
                }

                // Letterboxing only appears in breaks, so need to compare to other maps that have breaks rather than the reference.
                foreach (Beatmap otherBeatmap in aBeatmapSet.beatmaps)
                {
                    if (otherBeatmap.breaks.Count > 0)
                    {
                        if (beatmap.breaks.Count > 0 && otherBeatmap.breaks.Count > 0)
                        {
                            if (beatmap.generalSettings.letterbox != otherBeatmap.generalSettings.letterbox)
                                yield return new Issue(GetTemplate("Warning"), beatmap,
                                    "letterbox", otherBeatmap);
                        }

                        break;
                    }
                }

                // Widescreen support does nothing without a storyboard.
                if (beatmap.HasDifficultySpecificStoryboard() &&
                    refBeatmap.HasDifficultySpecificStoryboard() ||
                    aBeatmapSet.osb != null)
                {
                    if (beatmap.generalSettings.widescreenSupport != refBeatmap.generalSettings.widescreenSupport)
                        yield return new Issue(GetTemplate("Warning"), beatmap,
                            "widescreen support", refBeatmap);

                    if (beatmap.generalSettings.storyInFrontOfFire != refBeatmap.generalSettings.storyInFrontOfFire)
                        yield return new Issue(GetTemplate("Warning"), beatmap,
                            "storyboard in front of combo fire", refBeatmap);

                    if (beatmap.generalSettings.useSkinSprites != refBeatmap.generalSettings.useSkinSprites)
                        yield return new Issue(GetTemplate("Warning"), beatmap,
                            "usage of skin sprites in storyboard", refBeatmap);
                }
                else if (
                    aBeatmapSet.osb == null &&
                    (beatmap.HasDifficultySpecificStoryboard() ||
                    refBeatmap.HasDifficultySpecificStoryboard()))
                {
                    yield return new Issue(GetTemplate("Warning"), beatmap,
                        "storyboard", refBeatmap);
                }

                // Epilepsy warning requires either a storyboard or video to show.
                if (beatmap.HasDifficultySpecificStoryboard() &&
                    refBeatmap.HasDifficultySpecificStoryboard() ||
                    aBeatmapSet.osb != null ||
                    beatmap.videos.Count > 0 && refBeatmap.videos.Count > 0)
                {
                    if (beatmap.generalSettings.epilepsyWarning != refBeatmap.generalSettings.epilepsyWarning)
                        yield return new Issue(GetTemplate("Warning"), beatmap,
                            "epilepsy warning", refBeatmap);
                }

                if (beatmap.generalSettings.audioLeadIn != refBeatmap.generalSettings.audioLeadIn)
                    yield return new Issue(GetTemplate("Warning"), beatmap,
                        "audio lead-in", refBeatmap);

                if (beatmap.generalSettings.skinPreference != refBeatmap.generalSettings.skinPreference)
                    yield return new Issue(GetTemplate("Warning"), beatmap,
                        "skin preference", refBeatmap);

                if (beatmap.difficultySettings.sliderTickRate != refBeatmap.difficultySettings.sliderTickRate)
                    yield return new Issue(GetTemplate("Minor"), beatmap,
                        "slider tick rate", refBeatmap);
            }
        }
    }
}
