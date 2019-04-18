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

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            for (int i = 0; i < aBeatmapSet.songFilePaths.Count; ++i)
            {
                string filePath = aBeatmapSet.songFilePaths[i].Substring(aBeatmapSet.songPath.Length + 1);
                string fileName = filePath.Split(new char[] { '/', '\\' }).Last().ToLower();

                if (!aBeatmapSet.IsFileUsed(filePath) && fileName != "thumbs.db")
                    yield return new Issue(GetTemplate("Unused"), null,
                        filePath);
            }
        }
    }
}
