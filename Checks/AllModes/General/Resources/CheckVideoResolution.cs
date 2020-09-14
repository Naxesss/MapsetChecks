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
    public class CheckVideoResolution : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Resources",
            Message = "Too high video resolution.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Keeping video files under a certain resolution threshold."
                },
                {
                    "Reasoning",
                    @"
                    Videos, having multiple frames stacked after each other, naturally take more file size than any regular background image. 
                    Because of this, the resolution threshold needs to be lower than for backgrounds in order to keep file size reasonable, 
                    even if it is possible to download without the video.
                    <note>
                        This is partly to ensure a reasonable load on the server, not only on the players' end, due to ranked content 
                        being downloaded more often.
                    </note>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Resolution",
                    new IssueTemplate(Issue.Level.Problem,
                        "\"{0}\" greater than 1280 x 720 ({1} x {2})",
                        "file name", "width", "height")
                    .WithCause(
                        "A video has a width exceeding 1280 pixels or a height exceeding 720 pixels.") },
                
                { "Leaves Folder",
                    new IssueTemplate(Issue.Level.Problem,
                        "\"{0}\" leaves the current song folder, which shouldn't ever happen.",
                        "file name")
                    .WithCause(
                        "The file path of a video starts with two dots.") },

                { "Missing",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" is missing" + Common.CHECK_MANUALLY_MESSAGE,
                        "file name")
                    .WithCause(
                        "A video referenced is not present.") },

                { "Exception",
                    new IssueTemplate(Issue.Level.Error,
                        Common.FILE_EXCEPTION_MESSAGE,
                        "file name", "exception")
                    .WithCause(
                        "An exception occurred trying to parse a video.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (Issue issue in Common.GetTagOsuIssues(
                beatmapSet,
                beatmap => beatmap.videos.Count > 0 ? beatmap.videos.Select(aVideo => aVideo.path) : null,
                templateArg => GetTemplate(templateArg),
                tagFile =>
                {
                    // Executes for each non-faulty video file used in one of the beatmaps in the set.
                    List<Issue> issues = new List<Issue>();
                    if (tagFile.file.Properties.VideoWidth > 1280 ||
                        tagFile.file.Properties.VideoHeight > 720)
                    {
                        issues.Add(new Issue(GetTemplate("Resolution"), null,
                            tagFile.templateArgs[0],
                            tagFile.file.Properties.VideoWidth,
                            tagFile.file.Properties.VideoHeight));
                    }

                    return issues;
                }))
            {
                // Returns issues from both non-faulty and faulty files.
                yield return issue;
            }
        }
    }
}
