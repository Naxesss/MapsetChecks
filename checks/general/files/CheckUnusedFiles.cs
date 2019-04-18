using MapsetParser.objects;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MapsetChecks.checks.general.files
{
    public class CheckUnusedFiles : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Files",
            Message = "Unused files.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Unused",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "\"{0}\"",
                        "path")
                    .WithCause(
                        "A file in the song folder is not used in any of the .osu or .osb files. " +
                        "Includes unused .osb files. Ignores thumbs.db.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            for (int i = 0; i < beatmapSet.songFilePaths.Count; ++i)
            {
                string filePath = beatmapSet.songFilePaths[i].Substring(beatmapSet.songPath.Length + 1);
                string fileName = filePath.Split(new char[] { '/', '\\' }).Last().ToLower();

                if (!beatmapSet.IsFileUsed(filePath) && fileName != "thumbs.db")
                    yield return new Issue(GetTemplate("Unused"), null,
                        filePath);
            }
        }
    }
}
