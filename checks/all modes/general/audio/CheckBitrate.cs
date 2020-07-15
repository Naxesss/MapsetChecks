using MapsetParser.objects;
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
    public class CheckBitrate : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Audio",
            Message = "Too high or low audio bitrate.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
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
                    OGG and MP3 files are typically compressed, unlike Wave, making too low bitrate a concern even for 
                    hit sounds using those formats. An upper limit for hit sound quality is not enforced due to their short 
                    length and small impact on file size, even with uncompressed formats like Wave."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Bitrate",
                    new IssueTemplate(Issue.Level.Problem,
                        "Average audio bitrate for \"{0}\", {1} kbps, is too {2}.",
                        "path", "bitrate", "high/low")
                    .WithCause(
                        "The average bitrate of an audio file (MP3 or OGG format) is either higher than 192 kbps " +
                        "or lower than 128 kbps.") },

                { "Hit Sound",
                    new IssueTemplate(Issue.Level.Warning,
                        "Average audio bitrate for \"{0}\", {1} kbps, is really low.",
                        "path", "bitrate")
                    .WithCause(
                        "Same as the other check, except only applies to hit sounds using the lower threshold.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            if (beatmapSet.GetAudioFilePath() != null)
                foreach (Issue issue in GetIssue(beatmapSet, beatmapSet.GetAudioFilePath()))
                    yield return issue;

            foreach (string hitSoundFile in beatmapSet.hitSoundFiles)
            {
                string hitSoundPath = Path.Combine(beatmapSet.songPath, hitSoundFile);
                ManagedBass.ChannelType hitSoundFormat = Audio.GetFormat(hitSoundPath);
                if ((hitSoundFormat & ManagedBass.ChannelType.OGG) != 0 &&
                    (hitSoundFormat & ManagedBass.ChannelType.MP3) != 0)
                    continue;

                foreach (Issue issue in GetIssue(beatmapSet, hitSoundPath, true))
                    yield return issue;
            }
        }

        public IEnumerable<Issue> GetIssue(BeatmapSet beatmapSet, string audioPath, bool isHitSound = false)
        {
            // `Audio.GetBitrate` has a < 0.1 kbps error margin, so we should round this.
            double bitrate = Math.Round(Audio.GetBitrate(audioPath));
            // Hit sounds only need to follow the lower limit for quality requirements, as Wave
            // (which is the most used hit sound format currently) is otherwise uncompressed anyway.
            if (bitrate >= 128 && (bitrate <= 192 || isHitSound))
                yield break;

            string audioRelPath = PathStatic.RelativePath(audioPath, beatmapSet.songPath);
            if (!isHitSound)
                yield return new Issue(GetTemplate("Bitrate"), null,
                    audioRelPath, $"{bitrate:0.##}",
                    (bitrate < 128 ? "low" : "high"));
            else
                yield return new Issue(GetTemplate("Hit Sound"), null,
                    audioRelPath, $"{bitrate:0.##}");
        }
    }
}
