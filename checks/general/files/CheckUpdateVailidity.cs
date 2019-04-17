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

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            for (int i = 0; i < beatmapSet.songFilePaths.Count; ++i)
            {
                string myFilePath = beatmapSet.songFilePaths[i].Substring(beatmapSet.songPath.Length + 1);
                string myFileName = myFilePath.Split(new char[] { '/', '\\' }).Last().ToLower();

                if(myFileName.Length > 132)
                    yield return new Issue(GetTemplate("Too Long Name"), null,
                            myFilePath, myFileName.Length);
                
                if (myFileName.EndsWith(".osu"))
                {
                    bool myIsFine = false;

                    foreach (Beatmap myBeatmap in beatmapSet.beatmaps)
                    {
                        string myFileNameExpected = myBeatmap.GetOsuFileName();
                        if (myFileName == myFileNameExpected)
                            myIsFine = true;
                    }

                    if (!myIsFine)
                        yield return new Issue(GetTemplate("Wrong Format"), null,
                            myFilePath);

                    // updating .osu files larger than 1 mb will cause the update to stop at the 1 mb mark
                    FileInfo myFileInfo = new FileInfo(beatmapSet.songFilePaths[i]);
                    double myApproxMB = Math.Round(myFileInfo.Length / 10000d) / 100;
                    string myApproxMBString = (myApproxMB).ToString(CultureInfo.InvariantCulture);

                    if (myApproxMB > 1)
                        yield return new Issue(GetTemplate("File Size"), null,
                            myFilePath, myApproxMBString);
                }
            }
        }
    }
}
