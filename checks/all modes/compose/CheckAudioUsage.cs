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
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing audio files from being much longer than the beatmaps they are used for.
                    <image>
                        https://i.imgur.com/n8bYgaP.png
                        A beatmap which has left a large portion of the song unmapped.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Audio files tend to be large in file size, so allowing them to be much longer than beatmaps would be a waste of resources, 
                    since few linger around score screens for that long. Something needs to be happening in the storyboard or video, to keep 
                    people around from skipping to the score screen, to justify the audio file being longer."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Without Video/Storyboard",
                    new IssueTemplate(Issue.Level.Problem,
                        "Currently {0}% unused audio.",
                        "percent")
                    .WithCause(
                        "The amount of time after the last object exceeds 20% of the length of the audio file. " +
                        "No storyboard or video is present.") },

                { "With Video/Storyboard",
                    new IssueTemplate(Issue.Level.Warning,
                        "Currently {0}% unused audio. Ensure this is being occupied by the video or storyboard.",
                        "percent")
                    .WithCause(
                        "Same as the other check, except with a storyboard or video present.") }
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
                        string roundedPercentage = $"{unusedPercentage * 100:0.##}";
                        string templateKey = (hasStoryboard || hasVideo ? "With" : "Without") + " Video/Storyboard";

                        yield return new Issue(GetTemplate(templateKey), beatmap,
                            roundedPercentage);
                    }
                }
            }
        }
    }
}
