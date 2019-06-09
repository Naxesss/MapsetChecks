using MapsetChecks.objects;
using MapsetParser.objects;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.general.audio
{
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
                    </note>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "CBR",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "Audio bitrate for CBR encoded \"{0}\", {1} kbps, is too {2}.",
                        "path", "bitrate", "high/low")
                    .WithCause(
                        "The bitrate of the audio file is constant and is either higher than 192 kbps or lower than 128 kbps.") },

                { "VBR",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "Average audio bitrate for VBR encoded \"{0}\", {1} kbps (ranging from {2} to {3} kbps), is too {4}.",
                        "path", "average bitrate", "minimum bitrate", "maximum bitrate", "high/low")
                    .WithCause(
                        "The bitrate of the audio file is variable and the average bitrate rounds to a value either higher than 192 kbps or lower than 128 kbps.") },

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

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            if (aBeatmapSet.GetAudioFilePath() != null)
            {
                AudioFile file = new AudioFile(aBeatmapSet.GetAudioFilePath());

                // gets the bitrate in bps, so turn it into kbps
                double bitrate = file.GetAverageBitrate() / 1000;
                double minBitrate = file.GetLowestBitrate() / 1000;
                double maxBitrate = file.GetHighestBitrate() / 1000;

                if (minBitrate == maxBitrate)
                {
                    if (minBitrate < 128 || maxBitrate > 192)
                        yield return new Issue(GetTemplate("CBR"), null,
                            aBeatmapSet.GetAudioFileName(), bitrate.ToString(CultureInfo.InvariantCulture),
                            (bitrate < 128 ? "low" : "high"));
                }
                else
                {
                    if (bitrate < 128 || bitrate > 192)
                    {
                        if (Math.Round(bitrate) < 128 || Math.Round(bitrate) > 192)
                        {
                            yield return new Issue(GetTemplate("VBR"), null,
                                aBeatmapSet.GetAudioFileName(), bitrate.ToString(CultureInfo.InvariantCulture),
                                minBitrate.ToString(CultureInfo.InvariantCulture),
                                maxBitrate.ToString(CultureInfo.InvariantCulture),
                                (bitrate < 128 ? "low" : "high"));
                        }
                        else
                        {
                            yield return new Issue(GetTemplate("Exact VBR"), null,
                                aBeatmapSet.GetAudioFileName(), bitrate.ToString(CultureInfo.InvariantCulture),
                                minBitrate.ToString(CultureInfo.InvariantCulture),
                                maxBitrate.ToString(CultureInfo.InvariantCulture),
                                (bitrate < 128 ? "low" : "high"));
                        }
                    }
                }
            }
        }
    }
}
