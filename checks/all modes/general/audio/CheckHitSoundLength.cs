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
using ManagedBass;

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
                    Some soundcards have issues playing audio files which are less than 100 ms in length. Muted hit sounds are 
                    fine having 0 ms duration though, since they don't play audio anyway.
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
                        "A hit sound file is less than 100 ms long, but greater than 0 ms to allow for muted hit sound files.") },

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
                string hsPath = Path.Combine(aBeatmapSet.songPath, hsFile);

                double duration = 0;
                Exception exception = null;
                try
                {
                    duration = Audio.GetDuration(hsPath);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                if (exception == null)
                {
                    // Greater than 0 since 44-byte muted hit sounds are fine.
                    if (duration <= 100 && duration > 0)
                        yield return new Issue(GetTemplate("Length"), null,
                            hsFile, $"{duration:0.##}");
                }
                else
                    yield return new Issue(GetTemplate("Unable to check"), null,
                        hsFile, String.Join(" ", exception));
            }
        }
    }
}
