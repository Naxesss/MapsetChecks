using MapsetParser.objects;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;

namespace MapsetChecks.checks.examples
{
    public class GeneralCheckTest : GeneralCheck
    {
        /// <summary> Determines which modes the check shows for, in which category the check appears, the message for the check, etc. </summary>
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Test",
            Message = "Difficulty names are present in the beatmap.",
            Author = "Naxess",
            Documentation = new Dictionary<string, string>()
            {
                { "Purpose", "Show an example of a custom general check." },
                { "Reasoning", "Examples can teach through practice." }
            }
        };

        /// <summary> Returns a dictionary of issue templates, which determine how each sub-issue is formatted, the issue level, etc. </summary>
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                {
                    "DiffName",
                    new IssueTemplate(Issue.Level.Warning,
                        "One of the difficulty names is {0}.",
                        "difficulty name")
                }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach(Beatmap beatmap in beatmapSet.beatmaps)
                yield return new Issue(GetTemplate("DiffName"), null, beatmap.metadataSettings.version);
        }
    }
}
