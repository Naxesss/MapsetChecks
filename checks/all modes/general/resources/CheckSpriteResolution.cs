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
    public class CheckSpriteResolution : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Resources",
            Message = "Too high sprite resolution.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing storyboard images from being extremely large."
                },
                {
                    "Reasoning",
                    @"
                    Unlike background images, storyboard images can be used to pan, zoom, scroll, rotate, etc, so they have more lenient 
                    limits in terms of resolution, but otherwise follow the same reasoning."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Resolution",
                    new IssueTemplate(Issue.Level.Problem,
                        "\"{0}\"",
                        "file name")
                    .WithCause(
                        "A storyboard image has a width height product exceeding 17,000,000 pixels.") },

                { "Resolution Animation Frame",
                    new IssueTemplate(Issue.Level.Problem,
                        "\"{0}\" (Animation Frame)",
                        "file name")
                    .WithCause(
                        "Same as the regular storyboard image check, except on one used in an animation.") },

                // parsing results
                { "Leaves Folder",
                    new IssueTemplate(Issue.Level.Problem,
                        "\"{0}\" leaves the current song folder, which shouldn't ever happen.",
                        "file name")
                    .WithCause(
                        "The file path of a storyboard image starts with two dots.") },

                { "Missing",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" is missing" + Common.CHECK_MANUALLY_MESSAGE,
                        "file name")
                    .WithCause(
                        "A storyboard image referenced is not present.") },

                { "Exception",
                    new IssueTemplate(Issue.Level.Error,
                        Common.FILE_EXCEPTION_MESSAGE,
                        "file name", "exception")
                    .WithCause(
                        "An exception occurred trying to parse a storyboard image.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            // .osu
            foreach (Issue issue in Common.GetTagOsuIssues(
                beatmapSet,
                beatmap => beatmap.sprites.Count > 0 ? beatmap.sprites.Select(aSprite => aSprite.path) : null,
                templateArg => GetTemplate(templateArg),
                tagFile =>
                {
                    // Executes for each non-faulty sprite file used in one of the beatmaps in the set.
                    List<Issue> issues = new List<Issue>();
                    if (tagFile.file.Properties.PhotoWidth * tagFile.file.Properties.PhotoHeight > 17000000)
                        issues.Add(new Issue(GetTemplate("Resolution"), null,
                            tagFile.templateArgs[0]));

                    return issues;
                }))
            {
                // Returns issues from both non-faulty and faulty files.
                yield return issue;
            }

            foreach (Issue issue in Common.GetTagOsuIssues(
                beatmapSet,
                beatmap => beatmap.animations.Count > 0 ? beatmap.animations.SelectMany(aAnimation => aAnimation.framePaths) : null,
                templateArg => GetTemplate(templateArg),
                tagFile =>
                {
                    List<Issue> issues = new List<Issue>();
                    if (tagFile.file.Properties.PhotoWidth * tagFile.file.Properties.PhotoHeight > 17000000)
                        issues.Add(new Issue(GetTemplate("Resolution Animation Frame"), null,
                            tagFile.templateArgs[0]));

                    return issues;
                }))
            {
                yield return issue;
            }

            // .osb
            foreach (Issue issue in Common.GetTagOsbIssues(
                beatmapSet,
                osb => osb.sprites.Count > 0 ? osb.sprites.Select(aSprite => aSprite.path) : null,
                templateArg => GetTemplate(templateArg),
                tagFile =>
                {
                    List<Issue> issues = new List<Issue>();
                    if (tagFile.file.Properties.PhotoWidth * tagFile.file.Properties.PhotoHeight > 17000000)
                        issues.Add(new Issue(GetTemplate("Resolution"), null,
                            tagFile.templateArgs[0]));

                    return issues;
                }))
            {
                yield return issue;
            }

            foreach (Issue issue in Common.GetTagOsbIssues(
                beatmapSet,
                osb => osb.animations.Count > 0 ? osb.animations.SelectMany(aAnimation => aAnimation.framePaths) : null,
                templateArg => GetTemplate(templateArg),
                tagFile =>
                {
                    List<Issue> issues = new List<Issue>();
                    if (tagFile.file.Properties.PhotoWidth * tagFile.file.Properties.PhotoHeight > 17000000)
                        issues.Add(new Issue(GetTemplate("Resolution Animation Frame"), null,
                            tagFile.templateArgs[0]));

                    return issues;
                }))
            {
                yield return issue;
            }
        }
    }
}
