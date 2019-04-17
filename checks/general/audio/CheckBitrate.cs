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
            Author = "Naxess"
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

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            if (beatmapSet.GetAudioFilePath() != null)
            {
                AudioFile file = new AudioFile(beatmapSet.GetAudioFilePath());

                // gets the bitrate in bps, so turn it into kbps
                double bitrate = file.GetAverageBitrate() / 1000;
                double minBitrate = file.GetLowestBitrate() / 1000;
                double maxBitrate = file.GetHighestBitrate() / 1000;

                if (minBitrate == maxBitrate)
                {
                    if (minBitrate < 128 || maxBitrate > 192)
                        yield return new Issue(GetTemplate("CBR"), null,
                            beatmapSet.GetAudioFileName(), bitrate.ToString(CultureInfo.InvariantCulture),
                            (bitrate < 128 ? "low" : "high"));
                }
                else
                {
                    if (bitrate < 128 || bitrate > 192)
                    {
                        if (Math.Round(bitrate) < 128 || Math.Round(bitrate) > 192)
                        {
                            yield return new Issue(GetTemplate("VBR"), null,
                                beatmapSet.GetAudioFileName(), bitrate.ToString(CultureInfo.InvariantCulture),
                                minBitrate.ToString(CultureInfo.InvariantCulture),
                                maxBitrate.ToString(CultureInfo.InvariantCulture),
                                (bitrate < 128 ? "low" : "high"));
                        }
                        else
                        {
                            yield return new Issue(GetTemplate("Exact VBR"), null,
                                beatmapSet.GetAudioFileName(), bitrate.ToString(CultureInfo.InvariantCulture),
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
