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
    public class CheckHitSoundImbalance : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Audio",
            Message = "Imbalanced hit sounds.",
            Author = "Naxess"
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

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (string hsFile in beatmapSet.hitsoundFiles)
            {
                AudioFile audioFile = new AudioFile(beatmapSet.songPath + "\\" + hsFile);
                
                string errorMessage =
                    audioFile.ReadWav(
                        out float[] left,
                        out float[] right);

                if (errorMessage == null)
                {
                    if (right != null)
                    {
                        double totalLeft = left.Select(aValue => Math.Abs(aValue)).Sum();
                        double totalRight = right.Select(aValue => Math.Abs(aValue)).Sum();

                        if (totalLeft / 2 > totalRight || totalRight / 2 > totalLeft)
                            yield return new Issue(GetTemplate("Imbalance"), null,
                                hsFile, (totalLeft - totalRight > 0 ? "left" : "right"));
                    }
                }
                else
                    yield return new Issue(GetTemplate("Unable to check"), null,
                        hsFile, errorMessage);
            }
        }
    }
}
