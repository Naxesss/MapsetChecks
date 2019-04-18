using MapsetParser.objects;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;

namespace MapsetChecks.checks.examples
{
    public class CheckTest : BeatmapCheck
    {
        /// <summary> Determines which modes the check shows for, in which category the check appears, the message for the check, etc. </summary>
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                Beatmap.Mode.Standard,
                Beatmap.Mode.Catch
            },
            Difficulties = new Beatmap.Difficulty[]
            {
                Beatmap.Difficulty.Easy,
                Beatmap.Difficulty.Normal,
                Beatmap.Difficulty.Hard
            },
            Category = "Test",
            Message = "Difficulty name is present in the beatmap.",
            Author = "Naxess",
            Documentation = new Dictionary<string, string>()
            {
                { "Purpose", "Show an example of a custom check." },
                { "Reasoning", "Examples teach through practice." }
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
                        "The difficulty name is {0}.",
                        "difficulty name")
                }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            yield return new Issue(GetTemplate("DiffName"), aBeatmap, aBeatmap.metadataSettings.version);
        }
    }
}
