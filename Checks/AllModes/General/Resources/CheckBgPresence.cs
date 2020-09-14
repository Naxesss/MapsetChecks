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
    public class CheckBgPresence : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Resources",
            Message = "Missing background.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring that each beatmap in a beatmapset has a background image present.
                    <image-right>
                        https://i.imgur.com/P9TdA7K.jpg
                        An example of a default non-seasonal background as shown in the editor.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Backgrounds help players recognize the beatmap, and the absence of one makes it look incomplete."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "All",
                    new IssueTemplate(Issue.Level.Problem,
                        "All difficulties are missing backgrounds.")
                    .WithCause(
                        "None of the difficulties have a background present.") },

                { "One",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} has no background.",
                        "difficulty")
                    .WithCause(
                        "One or more difficulties are missing backgrounds, but not all.") },

                { "Missing",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} is missing its background file, \"{1}\".",
                        "difficulty", "path")
                    .WithCause(
                        "A background file path is present, but no file exists where it is pointing.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            if (beatmapSet.beatmaps.All(beatmap => beatmap.backgrounds.Count == 0))
            {
                yield return new Issue(GetTemplate("All"), null);
                yield break;
            }

            foreach (Beatmap beatmap in beatmapSet.beatmaps)
            {
                if (beatmap.backgrounds.Count == 0)
                {
                    yield return new Issue(GetTemplate("One"), null,
                        beatmap.metadataSettings.version);
                    continue;
                }

                if (beatmapSet.songPath == null)
                    continue;

                foreach (Background bg in beatmap.backgrounds)
                {
                    string path = beatmapSet.songPath + Path.DirectorySeparatorChar + bg.path;
                    if (!File.Exists(path))
                        yield return new Issue(GetTemplate("Missing"), null,
                            beatmap.metadataSettings.version, bg.path);
                }
            }
        }
    }
}
