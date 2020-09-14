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
                { "Problem",
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

                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Uninherited line changes nothing, other than {1}, ensure this makes sense.",
                        "timestamp - ", "something not immediately obvious")
                    .WithCause(
                        "Same as the first check, but changes something that inherited lines cannot, yet isn't immediately obvious, " +
                        "i.e. omitting barline, correcting an omitted barline, or nightcore cymbals.") },

                { "Warning Inherited",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Uninherited line changes nothing that can't be changed with an inherited line, other than {1}, ensure this makes sense.",
                        "timestamp - ", "something not immediately obvious")
                    .WithCause(
                        "Same as the second check, but changes something that inherited lines cannot, yet isn't immediately obvious, " +
                        "i.e. omitting barline, correcting an omitted barline, or nightcore cymbals.") },

                { "Minor Inherited",
                    new IssueTemplate(Issue.Level.Minor,
                        "{0} Inherited line changes {1}.",
                        "timestamp - ", "nothing(, other than SV/sample settings, but affects nothing)")
                    .WithCause("An inherited line changes no settings.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (Issue issue in GetUninheritedLineIssues(beatmap))
                yield return issue;

            foreach (Issue issue in GetInheritedLineIssues(beatmap))
                yield return issue;
        }

        private IEnumerable<Issue> GetUninheritedLineIssues(Beatmap beatmap)
        {
            List<TimingLine> lines = beatmap.timingLines.ToList();
            for (int i = 1; i < lines.Count; ++i)
            {
                if (!(lines[i] is UninheritedLine currentLine))
                    continue;

                // Can't do lines[i - 1] since that could give a green line on the same offset, which we don't want.
                TimingLine previousLine = beatmap.GetTimingLine(currentLine.offset - 1);
                UninheritedLine previousUninheritedLine = beatmap.GetTimingLine<UninheritedLine>(currentLine.offset - 1);

                if (!DownbeatsAlign(beatmap, currentLine, previousUninheritedLine))
                    continue;

                bool changesNCCymbals = false;
                if (!NightcoreCymbalsAlign(beatmap, currentLine, previousUninheritedLine))
                    changesNCCymbals = true;

                bool omittingBarline = false;
                bool correctingBarline = false;
                if (CanOmitBarLine(beatmap))
                {
                    // e.g. red line used mid-measure to account for bpm change shouldn't create a barline, so it's omitted, but the
                    // end of the measure won't have a barline unless another red line is placed there to correct it, hence both used.
                    omittingBarline = currentLine.omitsBarLine;
                    correctingBarline = previousUninheritedLine.omitsBarLine && !BarLinesAlign(beatmap, currentLine, previousUninheritedLine);
                    // Omitting bar lines isn't commonly seen in standard, so it's likely that people will
                    // miss incorrect usages of it, hence warn if it's the only thing keeping it used.
                    if ((omittingBarline || correctingBarline) && beatmap.generalSettings.mode != Beatmap.Mode.Standard)
                        continue;
                }

                List<string> notImmediatelyObvious = new List<string>();
                if (omittingBarline)   notImmediatelyObvious.Add("omitting first barline");
                if (correctingBarline) notImmediatelyObvious.Add($"correcting the omitted barline at {Timestamp.Get(previousUninheritedLine.offset)}");
                if (changesNCCymbals)  notImmediatelyObvious.Add("nightcore mod cymbals");
                string notImmediatelyObviousStr = string.Join(" and ", notImmediatelyObvious);

                if (!IsLineUsed(beatmap, currentLine, previousLine))
                {
                    if (notImmediatelyObvious.Count == 0)
                        yield return new Issue(GetTemplate("Problem"),
                            beatmap, Timestamp.Get(currentLine.offset));
                    else
                        yield return new Issue(GetTemplate("Warning"),
                            beatmap, Timestamp.Get(currentLine.offset), notImmediatelyObviousStr);
                }
                else
                {
                    if (notImmediatelyObvious.Count == 0)
                        yield return new Issue(GetTemplate("Problem Inherited"),
                            beatmap, Timestamp.Get(currentLine.offset));
                    else
                        yield return new Issue(GetTemplate("Warning Inherited"),
                            beatmap, Timestamp.Get(currentLine.offset), notImmediatelyObviousStr);
                }
            }
        }

        private IEnumerable<Issue> GetInheritedLineIssues(Beatmap beatmap)
        {
            List<TimingLine> lines = beatmap.timingLines.ToList();
            for (int i = 1; i < lines.Count; ++i)
            {
                if (!(lines[i] is InheritedLine currentLine))
                    continue;
                
                TimingLine previousLine = lines[i - 1];

                // Since "used" only includes false positives, this will only result in false negatives,
                // hence the check will never say that a used line is unused.
                if (IsLineUsed(beatmap, currentLine, previousLine))
                    continue;

                // Avoids confusion in case the line actually does change something from the
                // previous, but just doesn't apply to anything.
                string changesDesc = "";
                if (!UsesSV(beatmap, currentLine, previousLine) && currentLine.svMult != previousLine.svMult)
                    changesDesc += "SV";
                if (!UsesSamples(beatmap, currentLine, previousLine) && SamplesDiffer(currentLine, previousLine))
                    changesDesc += (changesDesc.Length > 0 ? " and " : "") + "sample settings";
                changesDesc += changesDesc.Length > 0 ? ", but affects nothing" : "nothing";

                yield return new Issue(GetTemplate("Minor Inherited"),
                    beatmap, Timestamp.Get(currentLine.offset), changesDesc);
            }
        }

        /// <summary> Returns whether the offset aligns in such a way that one line is a multiple of 4 beats away
        /// from the other, and the BPM and timing signature (meter) is the same. </summary>
        private bool DownbeatsAlign(Beatmap beatmap, UninheritedLine line, UninheritedLine otherLine)
        {
            bool negligibleDownbeatOffset = GetBeatOffset(otherLine, line, otherLine.meter) <= 1;
            return
                otherLine.bpm == line.bpm &&
                otherLine.meter == line.meter &&
                negligibleDownbeatOffset;
        }

        /// <summary> Returns whether the bar lines from the first line align perfectly with those of the second.
        /// Assumes the two lines have identical BPM and meter, use <see cref="DownbeatsAlign"/> for that. </summary>
        private bool BarLinesAlign(Beatmap beatmap, UninheritedLine line, UninheritedLine otherLine) =>
            // Even differences in 1 ms would be visible since it'd make 2 barlines next to each other.
            GetBeatOffset(otherLine, line, otherLine.meter) == 0;

        /// <summary> Returns whether the offset aligns in such a way that one line is a multiple of 4 measures away
        /// from the other (1 measure = 4 beats in 4/4 meter). This first checks that the downbeat structure is the same.
        /// <br></br><br></br>
        /// In the Nightcore mod, cymbals can be heard every 4 measures. </summary>
        private bool NightcoreCymbalsAlign(Beatmap beatmap, UninheritedLine line, UninheritedLine otherLine) =>
            DownbeatsAlign(beatmap, line, otherLine) && GetBeatOffset(otherLine, line, 4 * otherLine.meter) <= 1;

        /// <summary> Returns the ms difference between two timing lines, where the timing lines reset offset every
        /// given number of beats. </summary>
        private double GetBeatOffset(UninheritedLine line, UninheritedLine nextLine, double beatModulo)
        {
            double beatsIn = (nextLine.offset - line.offset) / line.msPerBeat;
            double offset = beatsIn % beatModulo;

            return
                Math.Min(
                    Math.Abs(offset),
                    Math.Abs(offset - beatModulo)) *
                line.msPerBeat;
        }

        /// <summary> Returns whether the beatmap supports omitting bar lines. This is currently limited to taiko and mania. </summary>
        private bool CanOmitBarLine(Beatmap beatmap) =>
            beatmap.generalSettings.mode == Beatmap.Mode.Standard || // Standard includes converts to taiko and
                                                                     // mania, so it would technically be used.
            beatmap.generalSettings.mode == Beatmap.Mode.Taiko ||
            beatmap.generalSettings.mode == Beatmap.Mode.Mania;

        /// <summary> Returns whether a line is considered used. Only partially covers uninherited lines.
        /// <br></br><br></br>
        /// Conditions for an inherited line being used (simplified, only false positives, e.g. hold notes/spinners) <br></br>
        /// - Changes sampleset, custom index, or volume and there is an edge/body within the time frame <br></br>
        /// - Changes SV and there are sliders starting within the time frame or changes SV and the mode is mania <br></br>
        /// - Changes kiai (causes effects on screen during duration) </summary>
        private bool IsLineUsed(Beatmap beatmap, TimingLine currentLine, TimingLine previousLine) =>
            UsesSamples(beatmap, currentLine, previousLine) ||
            UsesSV(beatmap, currentLine, previousLine) ||
            currentLine.kiai != previousLine.kiai;

        /// <summary> Returns whether this section makes use of sample changes (i.e. volume, sampleset, or custom index). </summary>
        private bool UsesSamples(Beatmap beatmap, TimingLine currentLine, TimingLine previousLine) =>
            SectionContainsObject<HitObject>(beatmap, currentLine) && SamplesDiffer(currentLine, previousLine);

        /// <summary> Returns whether this section changes sample settings (i.e. volume, sampleset, or custom index). </summary>
        private bool SamplesDiffer(TimingLine currentLine, TimingLine previousLine) =>
            currentLine.sampleset != previousLine.sampleset ||
            currentLine.customIndex != previousLine.customIndex ||
            currentLine.volume != previousLine.volume;

        /// <summary> Returns whether this section is affected by SV changes. </summary>
        private bool UsesSV(Beatmap beatmap, TimingLine currentLine, TimingLine previousLine) =>
            CanUseSV(beatmap, currentLine) && currentLine.svMult != previousLine.svMult;

        /// <summary> Returns whether changes to SV for the line will be used. </summary>
        private bool CanUseSV(Beatmap beatmap, TimingLine line) =>
            SectionContainsObject<Slider>(beatmap, line) ||
            // Taiko and mania affect approach rate through SV.
            beatmap.generalSettings.mode == Beatmap.Mode.Taiko ||
            beatmap.generalSettings.mode == Beatmap.Mode.Mania;

        /// <summary> Returns whether this section contains the respective hit object type.
        /// Only counts the start of objects. </summary>
        private bool SectionContainsObject<T>(Beatmap beatmap, TimingLine line) where T : HitObject
        {
            TimingLine nextLine = beatmap.GetNextTimingLine(line.offset);
            double nextSectionEnd = nextLine?.offset ?? beatmap.GetPlayTime();
            double objectTimeBeforeEnd = beatmap.GetPrevHitObject<T>(nextSectionEnd)?.time ?? 0;

            return objectTimeBeforeEnd >= line.offset;
        }
    }
}
