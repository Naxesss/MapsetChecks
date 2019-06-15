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
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Reducing the overall file size of beatmapset folders and preventing potentially malicious files 
                    from reaching the player."
                },
                {
                    "Reasoning",
                    @"
                    Having useless files in the folder that few players will look at in no way contributes to the gameplay experience 
                    and is a waste of resources. Unlike things like pointless bookmarks and green lines, files typically take up a way 
                    more noticeable amount of space.
                    <note>
                        For comparison, you'd need about 10 000 bookmarks, if not more, to match the file size of 
                        a regular hit sound.
                    </note>
                    <br \>
                    Official content distributing malicious .exe files or similar would also not reflect very well upon the game."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Unused",
                    new IssueTemplate(Issue.Level.Problem,
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
