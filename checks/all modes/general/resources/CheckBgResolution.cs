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
    public class CheckBgResolution : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Resources",
            Message = "Too high or low background resolution.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing background quality from being noticably low or unnoticably high to save on file size.
                    <image-right>
                        https://i.imgur.com/VrKRzse.png
                        The left side is ~2.25x the resolution of the right side, which is the equivalent of comparing 
                        2560 x 1440 to 1024 x 640.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Anything less than 1024 x 640 is usually quite noticeable, whereas anything higher than 2560 x 1440 
                    is unlikely to be visible with the setup of the average player.
                    <note>
                        This uses 16:10 as base, since anything outside of 16:9 will be cut off on that aspect ratio 
                        rather than resized to fit the screen, preserving quality.
                    </note>"
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Too high",
                    new IssueTemplate(Issue.Level.Problem,
                        "\"{0}\" greater than 2560 x 1440 ({1} x {2})",
                        "file name", "width", "height")
                    .WithCause(
                        "A background file has a width exceeding 2560 pixels or a height exceeding 1440 pixels.") },

                { "Very low",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" lower than 1024 x 640 ({1} x {2})",
                        "file name", "width", "height")
                    .WithCause(
                        "A background file has a width lower than 1024 pixels or a height lower than 640 pixels.") },

                { "File size",
                    new IssueTemplate(Issue.Level.Problem,
                        "\"{0}\" has a file size exceeding 2.5 MB ({1} MB)",
                        "file name", "file size")
                    .WithCause(
                        "A background file has a file size greater than 2.5 MB.") },

                // parsing results
                { "Leaves Folder",
                    new IssueTemplate(Issue.Level.Problem,
                        "\"{0}\" leaves the current song folder, which shouldn't ever happen.",
                        "file name")
                    .WithCause(
                        "The file path of a background file starts with two dots.") },

                { "Missing",
                    new IssueTemplate(Issue.Level.Problem,
                        "\"{0}\" is missing, so unable to check that.",
                        "file name")
                    .WithCause(
                        "A background file referenced is not present.") },

                { "Exception",
                    new IssueTemplate(Issue.Level.Error,
                        "\"{0}\" returned exception \"{1}\", so unable to check that.",
                        "file name", "exception")
                    .WithCause(
                        "An exception occurred trying to parse a background file.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            foreach (Issue issue in Common.GetTagOsuIssues(
                aBeatmapSet,
                aBeatmap => aBeatmap.backgrounds.Count > 0 ? aBeatmap.backgrounds.Select(aBg => aBg.path) : null,
                aTemplateArg => GetTemplate(aTemplateArg),
                aTagFile =>
                {
                    // Executes for each non-faulty background file used in one of the beatmaps in the set.
                    List<Issue> issues = new List<Issue>();
                    if (aTagFile.file.Properties.PhotoWidth > 2560 ||
                        aTagFile.file.Properties.PhotoHeight > 1440)
                    {
                        issues.Add(new Issue(GetTemplate("Too high"), null,
                            aTagFile.templateArgs[0],
                            aTagFile.file.Properties.PhotoWidth,
                            aTagFile.file.Properties.PhotoHeight));
                    }

                    else if (
                        aTagFile.file.Properties.PhotoWidth < 1024 ||
                        aTagFile.file.Properties.PhotoHeight < 640)
                    {
                        issues.Add(new Issue(GetTemplate("Very low"), null,
                            aTagFile.templateArgs[0],
                            aTagFile.file.Properties.PhotoWidth,
                            aTagFile.file.Properties.PhotoHeight));
                    }

                    // Most operating systems define 1 KB as 1024 B and 1 MB as 1024 KB,
                    // not 10^(3x) which the prefixes usually mean, but 2^(10x), since binary is more efficient for circuits,
                    // so since this is what your computer uses we'll use this too.
                    double megaBytes = new FileInfo(aTagFile.file.Name).Length / Math.Pow(1024, 2);
                    if (megaBytes > 2.5)
                    {
                        issues.Add(new Issue(GetTemplate("File size"), null,
                            aTagFile.templateArgs[0],
                            FormattableString.Invariant($"{megaBytes:0.##}")));
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
