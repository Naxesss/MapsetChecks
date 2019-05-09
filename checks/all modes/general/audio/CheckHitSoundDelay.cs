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
    public class CheckHitSoundDelay : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Audio",
            Message = "Delayed hit sounds.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Delay",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" has a delay of ~{1} ms.",
                        "path", "delay")
                    .WithCause(
                        "A hit sound file has very low volume for 4.5 ms or more.") },

                { "Minor Delay",
                    new IssueTemplate(Issue.Level.Minor,
                        "\"{0}\" has a delay of ~{1} ms.",
                        "path", "delay")
                    .WithCause(
                        "Same as the regular delay, except anything between 0.5 to 4.5 ms.") },

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
                AudioFile audioFile = new AudioFile(aBeatmapSet.songPath + "\\" + hsFile);
                
                string errorMessage =
                    audioFile.ReadWav(
                        out float[] left,
                        out float[] right);

                if (errorMessage == null)
                {
                    if (left.Length > 0 && (left.Max() > 0 || left.Min() < 0))
                    {
                        double maxStrength = left.Select(aValue => Math.Abs(aValue)).Max();
                        if (right != null)
                            maxStrength = (maxStrength + right.Select(aValue => Math.Abs(aValue)).Max()) / 2;

                        int i = 0;
                        double strength = 0;
                        for (; i < left.Length; ++i)
                        {
                            if (right != null)
                                strength += (Math.Abs(left[i]) + Math.Abs(right[i])) / 2;
                            else
                                strength += Math.Abs(left[i]);

                            if (strength >= maxStrength / 2)
                                break;

                            strength *= 0.75;
                        }

                        double delay = i / (double)50;
                        string delayString = (Math.Round(delay * 100) / 100).ToString(CultureInfo.InvariantCulture);

                        if (Math.Round(delay) >= 5)
                            yield return new Issue(GetTemplate("Delay"), null,
                                hsFile, delayString);

                        else if (delay >= 0.5)
                            yield return new Issue(GetTemplate("Minor Delay"), null,
                                hsFile, delayString);
                    }
                    else
                    {
                        // file is muted, so there's no delay
                    }
                }
                else
                    yield return new Issue(GetTemplate("Unable to check"), null,
                        hsFile, errorMessage);
            }
        }
    }
}
