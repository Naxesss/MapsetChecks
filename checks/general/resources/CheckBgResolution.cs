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
            Author = "Naxess"
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Too high",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "\"{0}\" greater than 1920 x 1200 ({1} x {2})",
                        "file name", "width", "height")
                    .WithCause(
                        "A background file has a width exceeding 2560 pixels or a height exceeding 1440 pixels.") },

                { "Very low",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" lower than 1024 x 640 ({1} x {2})",
                        "file name", "width", "height")
                    .WithCause(
                        "A background file has a width lower than 1024 pixels or a height lower than 640 pixels.") },

                // parsing results
                { "Leaves Folder",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "\"{0}\" leaves the current song folder, which shouldn't ever happen.",
                        "file name")
                    .WithCause(
                        "The file path of a background file starts with two dots.") },

                { "Missing",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "\"{0}\" is missing, so unable to check that.",
                        "file name", "exception")
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

                    return issues;
                }))
            {
                // Returns issues from both non-faulty and faulty files.
                yield return issue;
            }
        }
    }
}
