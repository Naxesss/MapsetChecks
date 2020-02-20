using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MapsetChecks.checks.general.resources
{
    [Check]
    public class CheckMultipleVideo : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Resources",
            Message = "Multiple videos.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Making sure that any inconsistency in video is intentional and makes sense."
                },
                {
                    "Reasoning",
                    @"
                    When adding a guest difficulty or adding different mode difficulties, the mapset host may forget to ensure 
                    that the videos across beatmaps in the set are consistent. Using multiple videos can be fine in cases like 
                    compilations or if one difficulty is thematically different enough to warrant it, but do keep in mind that 
                    it takes up additional space.
                    <note>
                        For taiko, videos usually need to be modified in some way since they're only visible on the bottom half 
                        of the screen, so this check ignores any inconsistency with that mode from other modes.
                    </note>"
                }
            }
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

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            IEnumerable<Beatmap.Mode> modes = beatmapSet.beatmaps.Select(beatmap => beatmap.generalSettings.mode).Distinct();
            List<KeyValuePair<string, Beatmap.Mode>> modeVideoPairs = new List<KeyValuePair<string, Beatmap.Mode>>();

            foreach (Beatmap.Mode mode in modes)
            {
                IEnumerable<Beatmap> beatmaps = beatmapSet.beatmaps.Where(beatmap => beatmap.generalSettings.mode == mode);
                IEnumerable<string> videoFiles =
                    beatmaps.Select(beatmap =>
                        beatmap.videos
                            .FirstOrDefault()?.path ?? "None")
                            .Append(beatmapSet.osb?.videos.FirstOrDefault()?.path ?? "").Distinct();

                foreach (string videoFile in videoFiles)
                {
                    IEnumerable<Beatmap> issueBeatmaps =
                        beatmaps.Where(beatmap =>
                            beatmap.videos.FirstOrDefault()?.path == videoFile ||
                            beatmapSet.osb?.videos.FirstOrDefault()?.path == videoFile);

                    if (videoFiles.Count(file => file != "") > 1 && issueBeatmaps.Any())
                    {
                        string joinedBeatmaps = String.Join(" ", issueBeatmaps.Select(beatmap => beatmap));

                        yield return new Issue(GetTemplate("Same Mode"), null,
                            videoFile, joinedBeatmaps);
                    }

                    if (videoFile != "" && !modeVideoPairs.Any(pair => pair.Key == videoFile && pair.Value == mode))
                        modeVideoPairs.Add(new KeyValuePair<string, Beatmap.Mode>(videoFile, mode));
                }
            }

            List<Beatmap.Mode> iteratedModes = new List<Beatmap.Mode>();
            List<Beatmap.Mode> erroredModes = new List<Beatmap.Mode>();
            foreach (KeyValuePair<string, Beatmap.Mode> pair in modeVideoPairs)
            {
                iteratedModes.Add(pair.Value);
                foreach (KeyValuePair<string, Beatmap.Mode> otherPair in modeVideoPairs.Where(pair => !iteratedModes.Contains(pair.Value)))
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
