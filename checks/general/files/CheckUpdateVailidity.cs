using MapsetParser.objects;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.general.files
{
    public class CheckUpdateVailidity : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Files",
            Message = "Issues with updating or downloading.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "File Size",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "\"{0}\" will cut any update at the 1 MB mark ({1} MB), causing objects to disappear.",
                        "path", "file size")
                    .WithCause(
                        "A .osu file exceeds 1 MB in file size.") },

                { "Wrong Format",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" should be in the format \"Artist - Title (Creator) [Version].osu\" in order to receive updates.",
                        "path")
                    .WithCause(
                        "A .osu file is not named after the mentioned format using its respective properties.") },

                { "Too Long Name",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" has a file name longer than 132 characters ({1}), which causes the .osz to fail to unzip for some users.",
                        "path", "length")
                    .WithCause(
                        "A .osu file has a file name longer than 132 characters.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            for (int i = 0; i < aBeatmapSet.songFilePaths.Count; ++i)
            {
                string filePath = aBeatmapSet.songFilePaths[i].Substring(aBeatmapSet.songPath.Length + 1);
                string fileName = filePath.Split(new char[] { '/', '\\' }).Last().ToLower();

                if(fileName.Length > 132)
                    yield return new Issue(GetTemplate("Too Long Name"), null,
                            filePath, fileName.Length);
                
                if (fileName.EndsWith(".osu"))
                {
                    bool isExpected = false;

                    foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
                    {
                        string fileNameExpected = beatmap.GetOsuFileName();
                        if (fileName == fileNameExpected)
                            isExpected = true;
                    }

                    if (!isExpected)
                        yield return new Issue(GetTemplate("Wrong Format"), null,
                            filePath);

                    // Updating .osu files larger than 1 mb will cause the update to stop at the 1 mb mark
                    FileInfo fileInfo = new FileInfo(aBeatmapSet.songFilePaths[i]);
                    double approxMB = Math.Round(fileInfo.Length / 10000d) / 100;
                    string approxMBString = (approxMB).ToString(CultureInfo.InvariantCulture);

                    if (approxMB > 1)
                        yield return new Issue(GetTemplate("File Size"), null,
                            filePath, approxMBString);
                }
            }
        }
    }
}
