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
            List<TimingLine> lines = aBeatmap.timingLines.ToList();
            for (int i = 1; i < lines.Count; ++i)
            {
                if (!(lines[i] is UninheritedLine currentLine))
                    continue;

                // Can't do lines[i - 1] since that could give a green line on the same offset, which we don't want.
                TimingLine previousLine = aBeatmap.GetTimingLine(currentLine.offset - 1);
                UninheritedLine previousUninheritedLine = aBeatmap.GetTimingLine<UninheritedLine>(currentLine.offset - 1);

                if (!SameDownbeatStructure(aBeatmap, currentLine, previousUninheritedLine))
                    continue;

                if (CanOmitBarLine(aBeatmap) && currentLine.omitsBarLine)
                    continue;

                if (IsLineUsed(aBeatmap, currentLine, previousLine))
                {
                    // In the nightcore mod, every 4th (or whatever the meter is) downbeat
                    // has an added cymbal, so that can technically change things.
                    if (SameNightcoreCymbalStructure(aBeatmap, currentLine, previousUninheritedLine))
                        yield return new Issue(GetTemplate("Problem Nothing"),
                        aBeatmap, Timestamp.Get(currentLine.offset));
                    else
                        yield return new Issue(GetTemplate("Warning Nothing"),
                            aBeatmap, Timestamp.Get(currentLine.offset));
                }
                else
                {
                    if (SameNightcoreCymbalStructure(aBeatmap, currentLine, previousUninheritedLine))
                        yield return new Issue(GetTemplate("Problem Inherited"),
                            aBeatmap, Timestamp.Get(currentLine.offset));
                    else
                        yield return new Issue(GetTemplate("Warning Inherited"),
                            aBeatmap, Timestamp.Get(currentLine.offset));
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

                // Since "used" only includes false positives, this only includes false negatives,
                // hence the check will never say that a used line is unused.
                if (!IsLineUsed(aBeatmap, currentLine, previousLine))
                {
                    // Avoids confusion in case the line actually does change something from the
                    // previous, but just doesn't apply to anything.
                    string changesDesc = "";
                    if (!UsesSV(aBeatmap, currentLine, previousLine) && currentLine.svMult != previousLine.svMult)
                        changesDesc += "SV";
                    if (!UsesSamples(aBeatmap, currentLine, previousLine) && SamplesDiffer(currentLine, previousLine))
                        changesDesc += (changesDesc.Length > 0 ? " and " : "") + "sample settings";
                    changesDesc += changesDesc.Length > 0 ? ", but affects nothing" : "nothing";

                    yield return new Issue(GetTemplate("Minor Inherited"),
                        aBeatmap, Timestamp.Get(currentLine.offset), changesDesc);
                }
            }
        }

        /// <summary> Returns whether the offset aligns in such a way that one line is a multiple of 4 beats away
        /// from the other, and the BPM and timing signature (meter) is the same.
        /// <br></br><br></br>
        /// Offset alignment is much more strict for omitted bar lines in taiko and mania due to bars otherwise
        /// becoming thicker than normal. </summary>
        private bool SameDownbeatStructure(Beatmap aBeatmap, UninheritedLine aLine, UninheritedLine anOtherLine)
        {
            bool negligibleDownbeatOffset = GetBeatOffset(anOtherLine, aLine, anOtherLine.meter) <= 1;

            if (CanOmitBarLine(aBeatmap) && anOtherLine.omitsBarLine)
                negligibleDownbeatOffset = GetBeatOffset(anOtherLine, aLine, anOtherLine.meter) == 0;

            return
                anOtherLine.bpm == aLine.bpm &&
                anOtherLine.meter == aLine.meter &&
                negligibleDownbeatOffset;
        }

        /// <summary> Returns whether the offset aligns in such a way that one line is a multiple of 4 measures away
        /// from the other (1 measure = 4 beats in 4/4 meter). This first checks that the downbeat structure is the same.
        /// <br></br><br></br>
        /// In the Nightcore mod, cymbals can be heard every 4 measures. </summary>
        private bool SameNightcoreCymbalStructure(Beatmap aBeatmap, UninheritedLine aLine, UninheritedLine anOtherLine) =>
            SameDownbeatStructure(aBeatmap, aLine, anOtherLine) && GetBeatOffset(anOtherLine, aLine, 4 * anOtherLine.meter) <= 1;

        /// <summary> Returns the ms difference between two timing lines, where the timing lines reset offset every
        /// given number of beats. </summary>
        private double GetBeatOffset(UninheritedLine aLine, UninheritedLine aNextLine, double aBeatModulo)
        {
            double beatsIn = (aNextLine.offset - aLine.offset) / aLine.msPerBeat;
            double offset = beatsIn % aBeatModulo;

            return
                Math.Min(
                    Math.Abs(offset),
                    Math.Abs(offset - aBeatModulo)) *
                aLine.msPerBeat;
        }

        /// <summary> Returns whether the beatmap supports omitting bar lines. This is currently limited to taiko and mania. </summary>
        private bool CanOmitBarLine(Beatmap aBeatmap) =>
            aBeatmap.generalSettings.mode == Beatmap.Mode.Taiko ||
            aBeatmap.generalSettings.mode == Beatmap.Mode.Mania;

        /// <summary> Returns whether a line is considered used. Only partially covers uninherited lines.
        /// <br></br><br></br>
        /// Conditions for an inherited line being used (simplified, only false positives, e.g. hold notes/spinners) <br></br>
        /// - Changes sampleset, custom index, or volume and there is an edge/body within the time frame <br></br>
        /// - Changes SV and there are sliders starting within the time frame or changes SV and the mode is mania <br></br>
        /// - Changes kiai (causes effects on screen during duration) </summary>
        private bool IsLineUsed(Beatmap aBeatmap, TimingLine aCurrentLine, TimingLine aPreviousLine) =>
            UsesSamples(aBeatmap, aCurrentLine, aPreviousLine) ||
            UsesSV(aBeatmap, aCurrentLine, aPreviousLine) ||
            aCurrentLine.kiai != aPreviousLine.kiai;

        /// <summary> Returns whether this section makes use of sample changes (i.e. volume, sampleset, or custom index). </summary>
        private bool UsesSamples(Beatmap aBeatmap, TimingLine aCurrentLine, TimingLine aPreviousLine) =>
            SectionContainsObject<HitObject>(aBeatmap, aCurrentLine) && SamplesDiffer(aCurrentLine, aPreviousLine);

        /// <summary> Returns whether this section changes sample settings (i.e. volume, sampleset, or custom index). </summary>
        private bool SamplesDiffer(TimingLine aCurrentLine, TimingLine aPreviousLine) =>
            aCurrentLine.sampleset != aPreviousLine.sampleset ||
            aCurrentLine.customIndex != aPreviousLine.customIndex ||
            aCurrentLine.volume != aPreviousLine.volume;

        /// <summary> Returns whether this section is affected by SV changes. </summary>
        private bool UsesSV(Beatmap aBeatmap, TimingLine aCurrentLine, TimingLine aPreviousLine) =>
            CanUseSV(aBeatmap, aCurrentLine) && aCurrentLine.svMult != aPreviousLine.svMult;

        /// <summary> Returns whether changes to SV for the line will be used. </summary>
        private bool CanUseSV(Beatmap aBeatmap, TimingLine aLine) =>
            SectionContainsObject<Slider>(aBeatmap, aLine) ||
            // Taiko and mania affect approach rate through SV.
            aBeatmap.generalSettings.mode == Beatmap.Mode.Taiko ||
            aBeatmap.generalSettings.mode == Beatmap.Mode.Mania;

        /// <summary> Returns whether this section contains the respective hit object type.
        /// Only counts the start of objects. </summary>
        private bool SectionContainsObject<T>(Beatmap aBeatmap, TimingLine aLine) where T : HitObject
        {
            TimingLine nextLine = aBeatmap.GetNextTimingLine(aLine.offset);
            double nextSectionEnd = nextLine?.offset ?? aBeatmap.GetPlayTime();
            double objectTimeBeforeEnd = aBeatmap.GetPrevHitObject<T>(nextSectionEnd)?.time ?? 0;

            return objectTimeBeforeEnd >= aLine.offset;
        }
    }
}
