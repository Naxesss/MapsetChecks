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
    public class CheckHitSoundLength : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Audio",
            Message = "Too short hit sounds.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing issues with soundcards."
                },
                {
                    "Reasoning",
                    @"
                    Some soundcards have issues playing audio files which are less than 100 ms in length.
                    <image>
                        https://i.imgur.com/0CpU3Gh.png
                        A hit sound which is less than 100 ms long, as shown in Audacity.
                    </image>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Length",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" is less than 100 ms long ({1} ms).",
                        "path", "length")
                    .WithCause(
                        "A hit sound file is less than 100 ms long.") },

                { "Unable to check",
                    new IssueTemplate(Issue.Level.Error,
                        "\"{0}\" {1}, so unable to check that.",
                        "path", "error")
                    .WithCause(
                        "There was an error parsing a hit sound file.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            foreach (string hsFile in aBeatmapSet.hitSoundFiles)
            {
                AudioFile audioFile = new AudioFile(aBeatmapSet.songPath + Path.DirectorySeparatorChar + hsFile);

                string errorMessage =
                    audioFile.ReadWav(
                        out float[] left,
                        out float[] right);

                if (errorMessage == null)
                {
                    if (left.Length > 0)
                    {
                        double length = left.Length / (double)44100 * 1000;
                        if (length < 100)
                            yield return new Issue(GetTemplate("Length"), null,
                                hsFile, length);
                    }
                    else
                    {
                        // file is muted, so there's no length
                    }
                }
                else
                    yield return new Issue(GetTemplate("Unable to check"), null,
                        hsFile, errorMessage);
            }
        }
    }
}
