using MapsetChecks.objects;
using MapsetParser.objects;
using MapsetParser.statics;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.general.audio
{
    public class CheckMultipleAudio : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Audio",
            Message = "Multiple audio files.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring that each beatmapset only contains one audio file."
                },
                {
                    "Reasoning",
                    @"
                    Although this works well in-game, the website preview, metadata, tags, etc are all relying on that each 
                    beatmapset is based around a single song. As such, having multiple songs in a single beatmapset is not 
                    supported properly. Each song will also need its own spread, so having each set of difficulties in its 
                    own beatmapset makes things more organized."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Multiple",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0}",
                        "audio file : difficulties")
                    .WithCause(
                        "There is more than one audio file used between all difficulties.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            foreach (Issue issue in Common.GetInconsistencies(
                    aBeatmapSet,
                    aBeatmap => PathStatic.RelativePath(aBeatmap.GetAudioFilePath(), aBeatmap.songPath),
                    GetTemplate("Multiple")))
                yield return issue;
        }
    }
}
