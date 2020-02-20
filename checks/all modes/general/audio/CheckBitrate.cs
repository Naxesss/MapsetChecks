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
                { "CBR",
                    new IssueTemplate(Issue.Level.Problem,
                        "Audio bitrate for CBR encoded \"{0}\", {1} kbps, is too {2}.",
                        "path", "bitrate", "high/low")
                    .WithCause(
                        "The bitrate of an audio file is constant and is either higher than 192 kbps or lower than 128 kbps.") },

                { "VBR",
                    new IssueTemplate(Issue.Level.Problem,
                        "Average audio bitrate for VBR encoded \"{0}\", {1} kbps (ranging from {2} to {3} kbps), is too {4}.",
                        "path", "average bitrate", "minimum bitrate", "maximum bitrate", "high/low")
                    .WithCause(
                        "The bitrate of the song audio file is variable and the average bitrate rounds to a value either higher " +
                        "than 192 kbps or lower than 128 kbps.") },

                { "CBR Hit Sound",
                    new IssueTemplate(Issue.Level.Warning,
                        "Audio bitrate for CBR encoded \"{0}\", {1} kbps, is really low.",
                        "path", "bitrate")
                    .WithCause(
                        "Same as the other check, except only applies to hit sounds using the OGG or MP3 format and also only uses " +
                        "the lower threshold.") },

                { "VBR Hit Sound",
                    new IssueTemplate(Issue.Level.Warning,
                        "Average audio bitrate for VBR encoded \"{0}\", {1} kbps (ranging from {2} to {3} kbps), is really low.",
                        "path", "average bitrate", "minimum bitrate", "maximum bitrate")
                    .WithCause(
                        "Same as the other check, except only applies to hit sounds using the OGG or MP3 format and also only uses " +
                        "the lower threshold.") },

                { "Exact VBR",
                    new IssueTemplate(Issue.Level.Minor,
                        "The exact average audio bitrate for VBR encoded \"{0}\", {1} kbps (ranging from {2} to {3} kbps), is too {4}. " +
                        "Although it barely makes a difference in this case.",
                        "path", "average bitrate", "minimum bitrate", "maximum bitrate", "high/low")
                    .WithCause(
                        "Same as for the regular VBR check, except does does not round." +
                        "<note>Some VBR encodings only slightly peak above 192 kbps and remain constant otherwise.</note>") }
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
                if ((hitSoundFormat & ManagedBass.ChannelType.OGG) == 0 ||
                    (hitSoundFormat & ManagedBass.ChannelType.MP3) == 0)
                {
                    // Hit sounds only need to follow the lower limit for quality requirements, as
                    // Wave (which is the most used hit sound format currently) is otherwise uncompressed anyway.
                    foreach (Issue issue in GetIssue(beatmapSet, hitSoundPath, true))
                        yield return issue;
                }
            }
        }

        public IEnumerable<Issue> GetIssue(BeatmapSet beatmapSet, string audioPath, bool isHitSound = false)
        {
            string audioRelPath = PathStatic.RelativePath(audioPath, beatmapSet.songPath);
            AudioFile file = new AudioFile(audioPath);

            // gets the bitrate in bps, so turn it into kbps
            double bitrate = file.GetAverageBitrate() / 1000;
            double minBitrate = file.GetLowestBitrate() / 1000;
            double maxBitrate = file.GetHighestBitrate() / 1000;

            if (minBitrate == maxBitrate)
            {
                if (minBitrate < 128 || (maxBitrate > 192 && !isHitSound))
                {
                    if(!isHitSound)
                        yield return new Issue(GetTemplate("CBR"), null,
                            audioRelPath, $"{bitrate:0.##}",
                            (bitrate < 128 ? "low" : "high"));
                    else
                        yield return new Issue(GetTemplate("CBR Hit Sound"), null,
                            audioRelPath, $"{bitrate:0.##}");
                }
            }
            else
            {
                if (bitrate < 128 || (bitrate > 192 && !isHitSound))
                {
                    if (Math.Round(bitrate) < 128 || (Math.Round(bitrate) > 192 && !isHitSound))
                    {
                        if (!isHitSound)
                            yield return new Issue(GetTemplate("VBR"), null,
                                audioRelPath,
                                $"{bitrate:0.##}", $"{minBitrate:0.##}", $"{maxBitrate:0.##}",
                                (bitrate < 128 ? "low" : "high"));
                        else
                            yield return new Issue(GetTemplate("VBR Hit Sound"), null,
                                audioRelPath,
                                $"{bitrate:0.##}", $"{minBitrate:0.##}", $"{maxBitrate:0.##}");
                    }
                    else if (!isHitSound)
                    {
                        yield return new Issue(GetTemplate("Exact VBR"), null,
                            audioRelPath,
                            $"{bitrate:0.##}", $"{minBitrate:0.##}", $"{maxBitrate:0.##}",
                            (bitrate < 128 ? "low" : "high"));
                    }
                }
            }
        }
    }
}
