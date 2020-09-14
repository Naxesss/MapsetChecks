using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using MapsetVerifierFramework.objects.resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.general.audio
{
    [Check]
    public class CheckAudioUsage : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Audio",
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
                    people around from skipping to the score screen, to justify the audio file being longer.

                    Note that this applies only to beatmapsets where <b>all</b> beatmaps exclude the last portion of the audio. If <b>any</b> 
                    uses it, then the audio does not need to be cut."
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
                        "Same as the other check, except with a storyboard or video present.") },

                { "Unable to check",
                    new IssueTemplate(Issue.Level.Error,
                        Common.FILE_EXCEPTION_MESSAGE,
                        "path", "exception info")
                    .WithCause(
                        "There was an error parsing the audio file.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            Dictionary<string, List<AudioUsage>> audioUsage = new Dictionary<string, List<AudioUsage>>();
            foreach (Beatmap beatmap in beatmapSet.beatmaps)
            {
                bool hasVideo = beatmap.videos.Count > 0;
                bool hasStoryboard =
                    beatmap.HasDifficultySpecificStoryboard() ||
                    (beatmapSet.osb?.IsUsed() ?? false);

                string audioPath = beatmap.GetAudioFilePath();
                if (audioPath == null)
                    continue;

                double duration = 0;
                Exception exception = null;
                try
                    { duration = Audio.GetDuration(audioPath); }
                catch (Exception ex)
                    { exception = ex; }

                if (exception != null)
                {
                    yield return new Issue(GetTemplate("Unable to check"), null,
                        PathStatic.RelativePath(audioPath, beatmap.songPath), Common.AsExceptionDiv(exception));
                    continue;
                }

                double lastEndTime = beatmap.hitObjects.LastOrDefault()?.GetEndTime() ?? 0;
                double fraction = lastEndTime / duration;

                if (!audioUsage.ContainsKey(audioPath))
                    audioUsage[audioPath] = new List<AudioUsage>();
                audioUsage[audioPath].Add(new AudioUsage(fraction, hasVideo || hasStoryboard));
            }
            
            foreach (string audioPath in audioUsage.Keys)
            {
                double maxFraction = 0;
                bool anyHasVideoOrSB = false;
                foreach (AudioUsage usage in audioUsage[audioPath])
                {
                    if (usage.fraction > maxFraction) maxFraction = usage.fraction;
                    if (usage.hasVideoOrSB)           anyHasVideoOrSB = true;
                }

                if (maxFraction > 0.8d)
                    continue;

                string templateKey = (anyHasVideoOrSB ? "With" : "Without") + " Video/Storyboard";

                yield return new Issue(GetTemplate(templateKey), null, $"{(1 - maxFraction) * 100:0.##}");
            }
        }
        
        public struct AudioUsage
        {
            public double fraction;
            public bool hasVideoOrSB;

            public AudioUsage(double fraction, bool hasVideoOrSB = false)
            {
                this.fraction = fraction;
                this.hasVideoOrSB = hasVideoOrSB;
            }
        }
    }
}
