using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MapsetChecks.checks.general.resources
{
    public class CheckMultipleVideo : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Resources",
            Message = "Multiple videos.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Same Mode",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} : {1}.",
                        "path", "difficulties")
                    .WithCause(
                        "Difficulties of the same mode do not share the same video.") },

                { "Cross Mode",
                    new IssueTemplate(Issue.Level.Warning,
                        "Inconsistent video between the {0} and {1} beatmaps.",
                        "mode", "mode")
                    .WithCause(
                        "Difficulties of separate modes (except taiko) do not share the same video.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            IEnumerable<Beatmap.Mode> modes = aBeatmapSet.beatmaps.Select(aBeatmap => aBeatmap.generalSettings.mode).Distinct();
            List<KeyValuePair<string, Beatmap.Mode>> modeVideoPairs = new List<KeyValuePair<string, Beatmap.Mode>>();

            foreach (Beatmap.Mode mode in modes)
            {
                IEnumerable<Beatmap> beatmaps = aBeatmapSet.beatmaps.Where(aBeatmap => aBeatmap.generalSettings.mode == mode);
                IEnumerable<string> videoFiles =
                    beatmaps.Select(aBeatmap =>
                        aBeatmap.videos
                            .FirstOrDefault()?.path ?? "None")
                            .Append(aBeatmapSet.osb?.videos.FirstOrDefault()?.path ?? "").Distinct();

                foreach (string videoFile in videoFiles)
                {
                    IEnumerable<Beatmap> issueBeatmaps =
                        beatmaps.Where(aBeatmap =>
                            aBeatmap.videos.FirstOrDefault()?.path == videoFile ||
                            aBeatmapSet.osb?.videos.FirstOrDefault()?.path == videoFile);

                    if (videoFiles.Count(aFile => aFile != "") > 1 && issueBeatmaps.Count() > 0)
                    {
                        string joinedBeatmaps = String.Join(" ", issueBeatmaps.Select(aBeatmap => aBeatmap));

                        yield return new Issue(GetTemplate("Same Mode"), null,
                            videoFile, joinedBeatmaps);
                    }

                    if (videoFile != "" && !modeVideoPairs.Any(aPair => aPair.Key == videoFile && aPair.Value == mode))
                        modeVideoPairs.Add(new KeyValuePair<string, Beatmap.Mode>(videoFile, mode));
                }
            }

            List<Beatmap.Mode> iteratedModes = new List<Beatmap.Mode>();
            List<Beatmap.Mode> erroredModes = new List<Beatmap.Mode>();
            foreach (KeyValuePair<string, Beatmap.Mode> pair in modeVideoPairs)
            {
                iteratedModes.Add(pair.Value);
                foreach (KeyValuePair<string, Beatmap.Mode> otherPair in modeVideoPairs.Where(aPair => !iteratedModes.Contains(aPair.Value)))
                {
                    // Ignore inconsistencies with taiko, as taiko generally does not include videos due to their playfield covering it
                    if (pair.Value != otherPair.Value
                        && pair.Key != otherPair.Key
                        && pair.Value != Beatmap.Mode.Taiko && otherPair.Value != Beatmap.Mode.Taiko
                        && !(erroredModes.Contains(pair.Value) && erroredModes.Contains(otherPair.Value)))
                    {
                        erroredModes.Add(pair.Value);
                        erroredModes.Add(otherPair.Value);

                        yield return new Issue(GetTemplate("Cross Mode"), null,
                            pair.Value.ToString().ToLower(), otherPair.Value.ToString().ToLower());
                    }
                }
            }
        }
    }
}
