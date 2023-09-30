﻿using System;
using System.Collections.Generic;
using System.IO;
using MapsetParser.objects;
using MapsetParser.statics;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using MapsetVerifierFramework.objects.resources;

namespace MapsetChecks.Checks.AllModes.General.Audio
{
    [Check]
    public class CheckBitrate : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata
        {
            Category = "Audio",
            Message = "Too high or low audio bitrate.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Preventing audio quality from being noticably low or unnoticably high to save on file size.
                    <image>
                        https://i.imgur.com/701cuCD.png
                        Audio bitrate as shown in the properties of a file. For some files this is not visible.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Once you get lower than 128 kbps the quality loss is usually quite noticeable. After 192 kbps, with the 
                    setup of the average player, it would be difficult to tell a difference and as such would also be a 
                    waste of resources.
                    <note>
                        Should no higher quality be available anywhere, less than 128 kbps may be acceptable depending on 
                        how noticeable it is.
                    </note>
                    <br \>
                    Audio files in the OGG container format using the Q6 setting will typically land around 192 kbps, 
                    somtimes inconveniently a little above that, so the upper limit for OGG is instead 208 kbps.
                    <note>
                        Q6 generally reads as 192 kbps in tools like Spek. These programs actually just approximate the 
                        bitrate by rounding to the closest 32 kbps step. 192 kbps is one such step. Therefore, any 
                        file > 208 kbps will show as 224 kbps in these tools.
                    </note>
                    <br \>
                    OGG and MP3 files are typically compressed, unlike Wave, making too low bitrate a concern even for 
                    hit sounds using those formats. An upper limit for hit sound quality is not enforced due to their short 
                    length and small impact on file size, even with uncompressed formats like Wave."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "Bitrate",
                    new IssueTemplate(Issue.Level.Problem,
                        "Average audio bitrate for \"{0}\", {1} kbps, is too {2}.",
                        "path", "bitrate", "high/low")
                    .WithCause(
                        "The average bitrate of an audio file is either higher than 192/208 kbps" +
                        "for MP3/OGG respectively or lower than 128 kbps.") },

                { "Hit Sound",
                    new IssueTemplate(Issue.Level.Warning,
                        "Average audio bitrate for \"{0}\", {1} kbps, is really low.",
                        "path", "bitrate")
                    .WithCause(
                        "Same as the other check, except only applies to hit sounds using the lower threshold.") },

                { "Exception",
                    new IssueTemplate(Issue.Level.Error,
                        Common.FILE_EXCEPTION_MESSAGE,
                        "path", "exception info")
                    .WithCause(
                        "A file which was attempted to be checked could not be opened.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var issue in GetAudioIssues(beatmapSet))
                yield return issue;
            
            foreach (var issue in GetHitSoundIssues(beatmapSet))
                yield return issue;
        }
        
        private IEnumerable<Issue> GetAudioIssues(BeatmapSet beatmapSet)
        {
            string audioPath = beatmapSet.GetAudioFilePath();
            if (audioPath == null)
                yield break;
            
            ManagedBass.ChannelType audioFormat = 0;
            Issue errorIssue = null;
            try
            {
                audioFormat = AudioBASS.GetFormat(audioPath);
            }
            catch (Exception exception)
            {
                errorIssue = new Issue(GetTemplate("Exception"), null,
                    PathStatic.RelativePath(audioPath, beatmapSet.songPath),
                    Common.ExceptionTag(exception));
            }

            if (errorIssue != null)
                yield return errorIssue;

            int upperBitrateLimit = 192;
            if ((audioFormat & ManagedBass.ChannelType.OGG) != 0)
                upperBitrateLimit = 208;

            // `Audio.GetBitrate` has a < 0.1 kbps error margin, so we should round this.
            double bitrate = Math.Round(AudioBASS.GetBitrate(audioPath));
            if (bitrate >= 128 && bitrate <= upperBitrateLimit)
                yield break;
            
            string relativePath = PathStatic.RelativePath(audioPath, beatmapSet.songPath);
            yield return new Issue(GetTemplate("Bitrate"), null,
                relativePath, $"{bitrate:0.##}",
                bitrate < 128 ? "low" : "high");
        }
        
        private IEnumerable<Issue> GetHitSoundIssues(BeatmapSet beatmapSet)
        {
            foreach (string hitSoundFile in beatmapSet.hitSoundFiles)
            {
                string hitSoundPath = Path.Combine(beatmapSet.songPath, hitSoundFile);

                ManagedBass.ChannelType hitSoundFormat = 0;
                Issue errorIssue = null;
                try
                {
                    hitSoundFormat = AudioBASS.GetFormat(hitSoundPath);
                }
                catch (Exception exception)
                {
                    errorIssue = new Issue(GetTemplate("Exception"), null,
                        PathStatic.RelativePath(hitSoundPath, beatmapSet.songPath),
                        Common.ExceptionTag(exception));
                }

                if (errorIssue != null)
                {
                    yield return errorIssue;
                    continue;
                }

                if ((hitSoundFormat & ManagedBass.ChannelType.OGG) != 0 &&
                    (hitSoundFormat & ManagedBass.ChannelType.MP3) != 0)
                {
                    continue;
                }

                double bitrate = Math.Round(AudioBASS.GetBitrate(hitSoundPath));
                // Hit sounds only need to follow the lower limit for quality requirements, as Wave
                // (which is the most used hit sound format currently) is otherwise uncompressed anyway.
                if (bitrate >= 128)
                    yield break;
                
                string relativePath = PathStatic.RelativePath(hitSoundPath, beatmapSet.songPath);
                yield return new Issue(GetTemplate("Hit Sound"), null,
                    relativePath, $"{bitrate:0.##}");
            }
        }
    }
}
