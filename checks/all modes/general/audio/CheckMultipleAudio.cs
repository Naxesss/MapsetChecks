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
    public class CheckMultipleAudio : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Audio",
            Message = "Multiple audio files.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Multiple",
                    new IssueTemplate(Issue.Level.Unrankable,
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
                    aBeatmap => aBeatmap.GetAudioFilePath(),
                    GetTemplate("Multiple")))
                yield return issue;
        }
    }
}
