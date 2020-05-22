using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MapsetChecks.checks.timing
{
    [Check]
    public class CheckFirstLine : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Timing",
            Message = "First line toggles kiai or is inherited.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing effects from happening and inherited lines before the first uninherited line."
                },
                {
                    "Reasoning",
                    @"
                    If you toggle kiai on the first line, then when the player starts the beatmap, kiai will instantly trigger and apply 
                    from the beginning until the next line. 
                    <image>
                        https://i.imgur.com/9F3LoR3.png
                        The game preventing you from enabling kiai on the first timing line.
                    </image>

                    If you place an inherited line before the first uninherited line, then the game will 
                    think the whole section isn't timed, causing the default bpm to be used and the inherited line to malfunction since 
                    it has nothing to inherit.
                    <image>
                        https://i.imgur.com/yqSEObl.png
                        The first line being inherited, as seen from the timing view.
                    </image>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Inherited",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} First timing line is inherited.",
                        "timestamp - ")
                    .WithCause(
                        "The first timing line of a beatmap is inherited.") },

                { "Toggles Kiai",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} First timing line toggles kiai.",
                        "timestamp - ")
                    .WithCause(
                        "The first timing line of a beatmap has kiai enabled.") },

                { "No Lines",
                    new IssueTemplate(Issue.Level.Problem,
                        "There are no timing lines.")
                    .WithCause(
                        "A beatmap has no timing lines.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            if (beatmap.timingLines.Count == 0)
            {
                yield return new Issue(GetTemplate("No Lines"), beatmap);
                yield break;
            }

            TimingLine line = beatmap.timingLines[0];

            if (!line.uninherited)
                yield return new Issue(GetTemplate("Inherited"), beatmap, Timestamp.Get(line.offset));
            else if (line.kiai)
                yield return new Issue(GetTemplate("Toggles Kiai"), beatmap, Timestamp.Get(line.offset));
        }
    }
}
