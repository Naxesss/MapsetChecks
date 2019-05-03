using MapsetChecks.objects;
using MapsetParser.objects;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.general.audio
{
    public class CheckAudioInVideo : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Audio",
            Message = "Audio channels in video.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Audio",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "\"{0}\"",
                        "file name")
                    .WithCause(
                        "An audio track is present in one of the video files.") },
                
                { "Leaves Folder",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "\"{0}\" leaves the current song folder, which shouldn't ever happen.",
                        "file name")
                    .WithCause(
                        "The file path of an audio file starts with two dots.") },

                { "Missing",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "\"{0}\" is missing, so unable to check that.",
                        "file name")
                    .WithCause(
                        "An audio file referenced is not present.") },

                { "Exception",
                    new IssueTemplate(Issue.Level.Error,
                        "\"{0}\" returned exception \"{1}\", so unable to check that.",
                        "file name", "exception")
                    .WithCause(
                        "An exception occurred trying to parse an audio file.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            foreach (Issue issue in Common.GetTagOsuIssues(
                aBeatmapSet,
                aBeatmap => aBeatmap.videos.Count > 0 ? aBeatmap.videos.Select(aVideo => aVideo.path) : null,
                aTemplateArg => GetTemplate(aTemplateArg),
                aTagFile =>
                {
                    // Executes for each non-faulty video file used in one of the beatmaps in the set.
                    List<Issue> issues = new List<Issue>();
                    if (
                            aTagFile.file.Properties.MediaTypes.HasFlag(TagLib.MediaTypes.Video) &&
                            aTagFile.file.Properties.AudioChannels > 0)
                        issues.Add(new Issue(GetTemplate("Audio"), null,
                            aTagFile.templateArgs[0]));

                    return issues;
                }))
            {
                // Returns issues from both non-faulty and faulty files.
                yield return issue;
            }
        }
    }
}
