using MapsetChecks.objects;
using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.compose
{
    public class CheckAudioUsage : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Compose",
            Message = "More than 20% unused audio at the end.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Without Video/Storyboard",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "Currently {0}% unused audio.",
                        "percent")
                    .WithCause(
                        "The amount of time after the last object exceeds 20% of the length of the audio file.") },

                { "With Video/Storyboard",
                    new IssueTemplate(Issue.Level.Warning,
                        "Currently {0}% unused audio. Ensure this is being occupied by the video or storyboard.",
                        "percent")
                    .WithCause(
                        "Same as without a video or storyboard.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
            {
                bool hasVideo      = beatmap.videos.Count > 0;
                bool hasStoryboard =
                    beatmap.HasDifficultySpecificStoryboard() ||
                    (aBeatmapSet.osb?.IsUsed() ?? false);

                string audioPath = beatmap.GetAudioFilePath();
                if (audioPath != null)
                {
                    AudioFile audioFile = new AudioFile(audioPath);

                    double audioDuration = audioFile.GetDuration();
                    double lastEndTime = beatmap.hitObjects.LastOrDefault()?.GetEndTime() ?? 0;

                    double unusedPercentage = 1 - lastEndTime / audioDuration;
                    if (unusedPercentage >= 0.2)
                    {
                        string roundedPercentage = (Math.Round(unusedPercentage * 1000) / 10.0f).ToString(CultureInfo.InvariantCulture);
                        string templateKey = (hasStoryboard || hasVideo ? "With" : "Without") + " Video/Storyboard";

                        yield return new Issue(GetTemplate(templateKey), beatmap,
                            roundedPercentage);
                    }
                }
            }
        }
    }
}
