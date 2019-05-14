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
    public class CheckBgPresence : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Resources",
            Message = "Missing background.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "All",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "All difficulties are missing backgrounds.")
                    .WithCause(
                        "None of the difficulties have a background present.") },

                { "One",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} has no background.",
                        "difficulty")
                    .WithCause(
                        "One or more difficulties are missing backgrounds, but not all.") },

                { "Missing",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} is missing its background file, \"{1}\".",
                        "difficulty", "path")
                    .WithCause(
                        "A background file path is present, but no file exists where it is pointing.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            if (aBeatmapSet.beatmaps.All(aBeatmap => aBeatmap.backgrounds.Count == 0))
                yield return new Issue(GetTemplate("All"), null);
            else
            {
                foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
                {
                    if (beatmap.backgrounds.Count == 0)
                        yield return new Issue(GetTemplate("One"), null,
                            beatmap.metadataSettings.version);

                    else if (aBeatmapSet.songPath != null)
                    {
                        foreach (Background bg in beatmap.backgrounds)
                        {
                            string path = aBeatmapSet.songPath + Path.DirectorySeparatorChar + bg.path;
                            if (!File.Exists(path))
                                yield return new Issue(GetTemplate("Missing"), null,
                                    beatmap.metadataSettings.version, bg.path);
                        }
                    }
                }
            }
        }
    }
}
