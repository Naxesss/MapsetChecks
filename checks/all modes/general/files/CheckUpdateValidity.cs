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
    public class CheckUpdateValidity : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Files",
            Message = "Issues with updating or downloading.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring that beatmaps can properly be downloaded and updated to their newest version.
                    <image>
                        https://i.imgur.com/7Nc9Ejr.png
                        An example of a song folder where one of the difficulties' file names are incorrect, causing it to be unable to update.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    By being unable to update a beatmap, potentially important fixes can easily be missed out on. This mostly 
                    affects players who download the map in qualified, as it is more visible to the public while not necessarily 
                    being in its final version.
                    <br \><br \>
                    For some Windows 10 users, file names longer than 132 characters cannot properly be unzipped by the game and 
                    simply vanish instead.
                    <image>
                        https://i.imgur.com/PO8eKvZ.png
                        A file name longer than 132 characters, caused by a combination of a long title and a long difficulty name.
                    </image>"
                }
            }
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
                        "\"{0}\" should be named \"{1}\" to receive updates.",
                        "file name", "artist - title (creator) [version].osu")
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
                string fileName = filePath.Split(new char[] { '/', '\\' }).Last();

                if(fileName.Length > 132)
                    yield return new Issue(GetTemplate("Too Long Name"), null,
                            filePath, fileName.Length);
                
                if (fileName.EndsWith(".osu"))
                {
                    Beatmap beatmap = aBeatmapSet.beatmaps.First(aBeatmap => aBeatmap.mapPath == filePath);
                    if (beatmap.GetOsuFileName().ToLower() != fileName.ToLower())
                        yield return new Issue(GetTemplate("Wrong Format"), null,
                            fileName, beatmap.GetOsuFileName());

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
