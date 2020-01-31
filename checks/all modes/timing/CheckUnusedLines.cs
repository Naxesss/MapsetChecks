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
using System.Linq;

namespace MapsetChecks.checks.timing
{
    [Check]
    public class CheckUnusedLines : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Timing",
            Message = "Unused timing lines.",
            Author = "Naxess",
            
            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring there are no unused timing lines in the beatmap."
                },
                {
                    "Reasoning",
                    @"
                    When placing uninherited lines on-beat with the previous uninherited line, timing may shift 1 ms forwards 
                    due to rounding errors. This means afer 20 uninherited lines placed in this way, timing may shift up to 
                    20 ms at the end. They may also affect the nightcore mod and main menu pulsing depending on placement.
                    <br \><br \>
                    Unused inherited lines don't cause any issues and are basically equivalent to bookmarks. Unless the mapper 
                    intended to do something with them (e.g. silencing slider ends/ticks), they can be safely removed, but 
                    removal is not necessary."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Problem Nothing",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} Uninherited line changes nothing.",
                        "timestamp - ")
                    .WithCause(
                        "An uninherited line is placed on a multiple of 4 downbeats away from the previous uninherited line, " +
                        "and changes no settings.") },

                { "Problem Inherited",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} Uninherited line changes nothing that can't be changed with an inherited line.",
                        "timestamp - ")
                    .WithCause(
                        "Same as the first check, but changes volume, sampleset, or another setting that an inherited line could change instead.") },

                { "Warning Nothing",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Uninherited line changes nothing, other than the finish with the nightcore mod. Ensure it makes sense to have a finish here.",
                        "timestamp - ")
                    .WithCause(
                        "Same as the first check, but is not on a multiple of 4 downbeats away from the previous uninherited line.") },

                { "Warning Inherited",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Uninherited line changes nothing that can't be changed with an inherited line, other than the finish with the nightcore mod. " +
                        "Ensure it makes sense to have a finish here.",
                        "timestamp - ")
                    .WithCause(
                        "An uninherited line is not placed on a multiple of 4 downbeats away from the previous uninherited line, " +
                        "and only changes settings which an inherited line could do instead.") },

                { "Minor Inherited",
                    new IssueTemplate(Issue.Level.Minor,
                        "{0} Inherited line changes {1}.",
                        "timestamp - ", "nothing(, other than SV/sample settings, but affects nothing)")
                    .WithCause("An inherited line changes no settings.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            foreach (Issue issue in GetUninheritedLineIssues(aBeatmap))
                yield return issue;

            foreach (Issue issue in GetInheritedLineIssues(aBeatmap))
                yield return issue;
        }

        private IEnumerable<Issue> GetUninheritedLineIssues(Beatmap aBeatmap)
        {
            // If the previous line omits the first barline in taiko and is less than a beat apart from the new one,
            // then the new one does change things even if it's just a ms ahead (prevents the barline from being
            // thicker than normal).
            bool canOmitBarLine =
                aBeatmap.generalSettings.mode == Beatmap.Mode.Taiko ||
                aBeatmap.generalSettings.mode == Beatmap.Mode.Mania;

            List<UninheritedLine> lines = aBeatmap.timingLines.OfType<UninheritedLine>().ToList();
            for (int i = 1; i < lines.Count; ++i)
            {
                bool negligibleDownbeatOffset = GetBeatOffset(lines[i - 1], lines[i], lines[i - 1].meter) <= 1;
                bool negligibleNightcoreCymbalOffset = GetBeatOffset(lines[i - 1], lines[i], 4 * lines[i - 1].meter) <= 1;

                if (canOmitBarLine && lines[i - 1].omitsBarLine)
                    negligibleDownbeatOffset = GetBeatOffset(lines[i - 1], lines[i], lines[i - 1].meter) == 0;

                // Uninherited lines 4 (or whatever the meter is) beats apart (varying up to 1 ms for rounding errors),
                // with the same bpm and meter, have the same downbeat structure. At which point the latter could be
                // replaced by an inherited line and function identically (other than the finish in the nightcore mod).
                if (lines[i - 1].bpm != lines[i].bpm ||
                    lines[i - 1].meter != lines[i].meter ||
                    !negligibleDownbeatOffset)
                {
                    continue;
                }

                // Check the lines in effect both here and before to see if an inherited
                // line is placed on top of the red line negating its changes.
                TimingLine previousLine = aBeatmap.GetTimingLine(lines[i].offset - 1);
                TimingLine currentLine = aBeatmap.GetTimingLine<UninheritedLine>(lines[i].offset);

                // If a line omits the first bar line we just treat it as used.
                if (canOmitBarLine && currentLine.omitsBarLine)
                    continue;

                if (previousLine.kiai == currentLine.kiai &&
                    previousLine.sampleset == currentLine.sampleset &&
                    previousLine.customIndex == currentLine.customIndex &&
                    previousLine.volume == currentLine.volume)
                {
                    // In the nightcore mod, every 4th (or whatever the meter is) downbeat
                    // has an added cymbal, so that technically changes things.
                    if (negligibleNightcoreCymbalOffset)
                        yield return new Issue(GetTemplate("Problem Nothing"),
                        aBeatmap, Timestamp.Get(lines[i].offset));
                    else
                        yield return new Issue(GetTemplate("Warning Nothing"),
                            aBeatmap, Timestamp.Get(lines[i].offset));
                }
                else
                {
                    if (negligibleNightcoreCymbalOffset)
                        yield return new Issue(GetTemplate("Problem Inherited"),
                            aBeatmap, Timestamp.Get(lines[i].offset));
                    else
                        yield return new Issue(GetTemplate("Warning Inherited"),
                            aBeatmap, Timestamp.Get(lines[i].offset));
                }
            }
        }

        private IEnumerable<Issue> GetInheritedLineIssues(Beatmap aBeatmap)
        {
            List<TimingLine> lines = aBeatmap.timingLines.ToList();
            for (int i = 1; i < lines.Count; ++i)
            {
                if (!(lines[i] is InheritedLine currentLine))
                    continue;

                TimingLine previousLine = lines[i - 1];
                TimingLine nextLine = aBeatmap.GetNextTimingLine(currentLine.offset);

                double timingSectionEnd = nextLine?.offset ?? aBeatmap.GetPlayTime();

                double prevEndTime = aBeatmap.GetPrevHitObject(timingSectionEnd)?.GetEndTime() ?? 0;
                double prevSliderStart = aBeatmap.GetPrevHitObject<Slider>(timingSectionEnd)?.time ?? 0;

                bool containsObjects = prevEndTime >= currentLine.offset;
                bool canAffectSV =
                    prevSliderStart >= currentLine.offset ||
                    // Taiko and mania affect approach rate through SV.
                    aBeatmap.generalSettings.mode == Beatmap.Mode.Mania ||
                    aBeatmap.generalSettings.mode == Beatmap.Mode.Taiko;

                bool sampleSettingsDiffer =
                    currentLine.sampleset != previousLine.sampleset ||
                    currentLine.customIndex != previousLine.customIndex ||
                    currentLine.volume != previousLine.volume;

                // Conditions for an inherited line being used (simplified, only false positives, e.g. hold notes/spinners)
                // - Changes sampleset, custom index, or volume and there is an edge/body within the time frame
                // - Changes SV and there are sliders starting within the time frame or changes SV and the mode is mania
                // - Changes kiai (causes effects on screen during duration)
                bool used =
                    containsObjects && sampleSettingsDiffer ||
                    canAffectSV && currentLine.svMult != previousLine.svMult ||
                    currentLine.kiai != previousLine.kiai;

                // Since "used" only includes false positives, this only includes false negatives,
                // hence the check will never say that a used line is unused.
                if (!used)
                {
                    // Avoids confusion in case the line actually does change something
                    // from the previous, but just doesn't apply to anything.
                    string changesDesc = "";
                    if (!canAffectSV && currentLine.svMult != previousLine.svMult)
                        changesDesc += "SV";
                    if (!containsObjects && sampleSettingsDiffer)
                        changesDesc += (changesDesc.Length > 0 ? " and " : "") + "sample settings";
                    changesDesc += changesDesc.Length > 0 ? ", but affects nothing" : "nothing";

                    yield return new Issue(GetTemplate("Minor Inherited"),
                        aBeatmap, Timestamp.Get(currentLine.offset), changesDesc);
                }
            }
        }

        /// <summary> Returns the ms difference between two timing lines, where the timing lines reset offset every given number of beats. </summary>
        private double GetBeatOffset(UninheritedLine aLine, UninheritedLine aNextLine, double aBeatOffset)
        {
            double beatsIn = (aNextLine.offset - aLine.offset) / aLine.msPerBeat;
            double offset = beatsIn % aBeatOffset;

            return
                Math.Min(
                    Math.Abs(offset),
                    Math.Abs(offset - aBeatOffset)) *
                aLine.msPerBeat;
        }
    }
}
