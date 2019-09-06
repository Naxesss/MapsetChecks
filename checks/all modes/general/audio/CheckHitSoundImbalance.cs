using MapsetParser.objects;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using MapsetVerifierFramework.objects.resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.general.audio
{
    [Check]
    public class CheckHitSoundImbalance : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Audio",
            Message = "Imbalanced hit sounds.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing the audio channels of hit sounds from being noticeably imbalanced, for example the left 
                    channel being twice as loud as the right.
                    <image>
                        https://i.imgur.com/6if5mJO.png
                        A hit sound which is much louder in the left channel compared to the right, as shown in Audacity.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Having noticeably imbalanced hit sounds is often jarring, especially if used frequently or consecutively."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Imbalance",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" has a notably louder {1} channel.",
                        "path", "left/right")
                    .WithCause(
                        "One of the channels of a hit sound has double the total volume of the other") },

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

                int channels = 0;
                List<float[]> peaks = null;
                Exception exception = null;
                try
                {
                    channels = Audio.GetChannels(hsPath);
                    peaks    = Audio.GetPeaks(hsPath);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                if (exception == null)
                {
                    // Mono can't be imbalanced since it's the same audio on both sides.
                    if (channels >= 2)
                    {
                        float leftSum = 0;
                        float rightSum = 0;

                        leftSum = peaks.Sum(aPeak => aPeak?[0] ?? 0);
                        rightSum = peaks.Sum(aPeak => aPeak.Count() > 1 ? aPeak?[1] ?? 0 : 0);
                        
                        if (leftSum / 2 > rightSum || rightSum / 2 > leftSum)
                            yield return new Issue(GetTemplate("Imbalance"), null,
                                hsFile, (leftSum - rightSum > 0 ? "left" : "right"));
                    }
                }
                else
                    yield return new Issue(GetTemplate("Unable to check"), null,
                        hsFile, String.Join(" ", exception));
            }
        }
    }
}
