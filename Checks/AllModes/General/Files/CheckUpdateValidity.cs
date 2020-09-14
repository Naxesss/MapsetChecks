using MapsetParser.objects;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.general.files
{
    [Check]
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
                    <br><br>
                    The name of the file seems to determine how osu initially checks the map for updates. This is then presumably 
                    stored in some local database because deleting and re-downloading doesn't seem to affect it. So if you 
                    already had the map when the file name was correct, it may properly update for you while showing ""not 
                    submitted"" for others downloading it for the first time.
                    <br><br>
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
                    new IssueTemplate(Issue.Level.Problem,
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
                    new IssueTemplate(Issue.Level.Minor,
                        "\"{0}\" has a file name longer than 132 characters ({1}), which causes the .osz to fail to unzip for a few users.",
                        "path", "length")
                    .WithCause(
                        "A .osu file has a file name longer than 132 characters.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            for (int i = 0; i < beatmapSet.songFilePaths.Count; ++i)
            {
                string filePath = beatmapSet.songFilePaths[i].Substring(beatmapSet.songPath.Length + 1);
                string fileName = filePath.Split(new char[] { '/', '\\' }).Last();

                if(fileName.Length > 132)
                    yield return new Issue(GetTemplate("Too Long Name"), null,
                            filePath, fileName.Length);

                if (!fileName.EndsWith(".osu"))
                    continue;

                Beatmap beatmap = beatmapSet.beatmaps.First(otherBeatmap => otherBeatmap.mapPath == filePath);
                if (beatmap.GetOsuFileName().ToLower() != fileName.ToLower())
                    yield return new Issue(GetTemplate("Wrong Format"), null,
                        fileName, beatmap.GetOsuFileName());

                // Updating .osu files larger than 1 mb will cause the update to stop at the 1 mb mark
                FileInfo fileInfo = new FileInfo(beatmapSet.songFilePaths[i]);
                double MB = fileInfo.Length / Math.Pow(1024, 2);

                if (MB > 1)
                    yield return new Issue(GetTemplate("File Size"), null,
                        filePath, $"{MB:0.##}");
            }
        }
    }
}
