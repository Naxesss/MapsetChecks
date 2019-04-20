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
    public class CheckSpriteResolution : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Resources",
            Message = "Too high sprite resolution.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Resolution",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "\"{0}\"",
                        "file name")
                    .WithCause(
                        "A storyboard image has a width height product exceeding 17,000,000 pixels.") },

                { "Resolution Animation Frame",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "\"{0}\" (Animation Frame)",
                        "file name")
                    .WithCause(
                        "Same as the regular storyboard image check, except on one used in an animation.") },

                // parsing results
                { "Leaves Folder",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "\"{0}\" leaves the current song folder, which shouldn't ever happen.",
                        "file name")
                    .WithCause(
                        "The file path of a storyboard image starts with two dots.") },

                { "Missing",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" is missing, so unable to check that.",
                        "file name", "exception")
                    .WithCause(
                        "A storyboard image referenced is not present.") },

                { "Exception",
                    new IssueTemplate(Issue.Level.Error,
                        "\"{0}\" returned exception \"{1}\", so unable to check that.",
                        "file name", "exception")
                    .WithCause(
                        "An exception occurred trying to parse a storyboard image.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            // .osu
            foreach (Issue issue in Common.GetTagOsuIssues(
                aBeatmapSet,
                aBeatmap => aBeatmap.sprites.Count > 0 ? aBeatmap.sprites.Select(aSprite => aSprite.path) : null,
                aTemplateArg => GetTemplate(aTemplateArg),
                aTagFile =>
                {
                    // Executes for each non-faulty sprite file used in one of the beatmaps in the set.
                    List<Issue> issues = new List<Issue>();
                    if (aTagFile.file.Properties.PhotoWidth * aTagFile.file.Properties.PhotoHeight > 17000000)
                        issues.Add(new Issue(GetTemplate("Resolution"), null,
                            aTagFile.templateArgs[0]));

                    return issues;
                }))
            {
                // Returns issues from both non-faulty and faulty files.
                yield return issue;
            }

            foreach (Issue issue in Common.GetTagOsuIssues(
                aBeatmapSet,
                aBeatmap => aBeatmap.animations.Count > 0 ? aBeatmap.animations.Select(aAnimation => aAnimation.path) : null,
                aTemplateArg => GetTemplate(aTemplateArg),
                aTagFile =>
                {
                    List<Issue> issues = new List<Issue>();
                    if (aTagFile.file.Properties.PhotoWidth * aTagFile.file.Properties.PhotoHeight > 17000000)
                        issues.Add(new Issue(GetTemplate("Resolution Animation Frame"), null,
                            aTagFile.templateArgs[0]));

                    return issues;
                }))
            {
                yield return issue;
            }

            // .osb
            foreach (Issue issue in Common.GetTagOsbIssues(
                aBeatmapSet,
                anOsb => anOsb.sprites.Count > 0 ? anOsb.sprites.Select(aSprite => aSprite.path) : null,
                aTemplateArg => GetTemplate(aTemplateArg),
                aTagFile =>
                {
                    List<Issue> issues = new List<Issue>();
                    if (aTagFile.file.Properties.PhotoWidth * aTagFile.file.Properties.PhotoHeight > 17000000)
                        issues.Add(new Issue(GetTemplate("Resolution"), null,
                            aTagFile.templateArgs[0]));

                    return issues;
                }))
            {
                yield return issue;
            }

            foreach (Issue issue in Common.GetTagOsbIssues(
                aBeatmapSet,
                anOsb => anOsb.animations.Count > 0 ? anOsb.animations.Select(aAnimation => aAnimation.path) : null,
                aTemplateArg => GetTemplate(aTemplateArg),
                aTagFile =>
                {
                    List<Issue> issues = new List<Issue>();
                    if (aTagFile.file.Properties.PhotoWidth * aTagFile.file.Properties.PhotoHeight > 17000000)
                        issues.Add(new Issue(GetTemplate("Resolution Animation Frame"), null,
                            aTagFile.templateArgs[0]));

                    return issues;
                }))
            {
                yield return issue;
            }
        }
    }
}
