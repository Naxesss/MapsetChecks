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
    public class CheckConcurrentLines : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Timing",
            Message = "Concurrent or conflicting timing lines.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing issues with concurrent lines of the same type, such as them switching order when loading the beatmap.
                    <image>
                        https://i.imgur.com/whTV4aV.png
                        Two inherited lines which were originally the other way around, but swapped places when opening the beatmap again.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Depending on how the game loads the lines, they may be loaded in the wrong order causing certain effects to disappear, 
                    like the editor to not see that kiai is enabled where it is in gameplay. This coupled with the fact that future versions 
                    of the game may change how these behave make them highly unreliable.
                    <note>
                        Two lines of different types, however, work properly as inherited and uninherited lines are handeled seperately, 
                        where the inherited will always apply its effects last.
                    </note>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Concurrent",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} Concurrent {1} lines.",
                        "timestamp - ", "inherited/uninherited")
                    .WithCause(
                        "Two inherited or uninherited timing lines exist at the same point in time.") },

                { "Conflicting",
                    new IssueTemplate(Issue.Level.Minor,
                        "{0} Conflicing line settings. Green: {1}. Red: {2}. {3}.",
                        "timestamp - ", "green setting(s)", "red setting(s)", "precedence")
                    .WithCause(
                        "An inherited and uninherited timing line exists at the same point in time and have different settings.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            // Since the list of timing lines is sorted by time we can just check the previous line.
            for (int i = 1; i < aBeatmap.timingLines.Count; ++i)
            {
                if (aBeatmap.timingLines[i - 1].offset == aBeatmap.timingLines[i].offset)
                {
                    if (aBeatmap.timingLines[i - 1].uninherited == aBeatmap.timingLines[i].uninherited)
                    {
                        string inheritance =
                            aBeatmap.timingLines[i].uninherited ?
                                "uninherited" : "inherited";

                        yield return new Issue(GetTemplate("Concurrent"), aBeatmap,
                            Timestamp.Get(aBeatmap.timingLines[i].offset), inheritance);
                    }
                    else if (
                        aBeatmap.timingLines[i - 1].kiai != aBeatmap.timingLines[i].kiai ||
                        aBeatmap.timingLines[i - 1].volume != aBeatmap.timingLines[i].volume ||
                        aBeatmap.timingLines[i - 1].sampleset != aBeatmap.timingLines[i].sampleset ||
                        aBeatmap.timingLines[i - 1].customIndex != aBeatmap.timingLines[i].customIndex)
                    {
                        string conflictingGreenSettings = "";
                        string conflictingRedSettings = "";

                        InheritedLine greenLine = null;
                        UninheritedLine redLine = null;

                        // We've guaranteed that one line is inherited and the other is
                        // uninherited, so we can figure out both by checking one.
                        string precedence = "";
                        if (aBeatmap.timingLines[i - 1] is InheritedLine)
                        {
                            greenLine = aBeatmap.timingLines[i - 1] as InheritedLine;
                            redLine = aBeatmap.timingLines[i] as UninheritedLine;
                            precedence = "Red overrides green";
                        }
                        else
                        {
                            greenLine = aBeatmap.timingLines[i] as InheritedLine;
                            redLine = aBeatmap.timingLines[i - 1] as UninheritedLine;
                            precedence = "Green overrides red";
                        }

                        if (greenLine.kiai != redLine.kiai)
                        {
                            conflictingGreenSettings += (conflictingGreenSettings.Length > 0 ? ", " : "") + (greenLine.kiai ? "kiai" : "no kiai");
                            conflictingRedSettings   += (conflictingRedSettings.Length > 0   ? ", " : "") + (redLine.kiai   ? "kiai" : "no kiai");
                        }
                        if (greenLine.volume != redLine.volume)
                        {
                            conflictingGreenSettings += (conflictingGreenSettings.Length > 0 ? ", " : "") + $"{greenLine.volume}% volume";
                            conflictingRedSettings   += (conflictingRedSettings.Length > 0   ? ", " : "") + $"{redLine.volume}% volume";
                        }
                        if (greenLine.sampleset != redLine.sampleset)
                        {
                            conflictingGreenSettings += (conflictingGreenSettings.Length > 0 ? ", " : "") + $"{greenLine.sampleset} sampleset";
                            conflictingRedSettings   += (conflictingRedSettings.Length > 0   ? ", " : "") + $"{redLine.sampleset} sampleset";
                        }
                        if (greenLine.customIndex != redLine.customIndex)
                        {
                            conflictingGreenSettings += (conflictingGreenSettings.Length > 0 ? ", " : "") + $"custom {greenLine.customIndex}";
                            conflictingRedSettings   += (conflictingRedSettings.Length > 0   ? ", " : "") + $"custom {redLine.customIndex}";
                        }

                        yield return new Issue(GetTemplate("Conflicting"), aBeatmap,
                            Timestamp.Get(aBeatmap.timingLines[i].offset),
                            conflictingGreenSettings, conflictingRedSettings,
                            precedence);
                    }
                }
            }
        }
    }
}
