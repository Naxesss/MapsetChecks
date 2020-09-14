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
    public class CheckBeforeLine : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            // Mania doesn't have this issue since SV affects scroll speed rather than properties of objects.
            Modes = new Beatmap.Mode[]
            {
                Beatmap.Mode.Standard,
                Beatmap.Mode.Taiko,
                Beatmap.Mode.Catch
            },

            Category = "Timing",
            Message = "Hit object is unaffected by a line very close to it.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing unintentional side effects like wrong snappings and slider velocities 
                    caused by hit objects or timing lines being slightly unsnapped."
                },
                {
                    "Reasoning",
                    @"
                    Sliders before a timing line (even if just by 1 ms or less), will not be affected by its slider velocity. 
                    With 1 ms unsnaps being common for objects and lines due to rounding errors when copy pasting, this in turn 
                    becomes a common issue for all game modes (excluding mania since SV works differently there).
                    <note>
                        If bpm changes, this will still keep track of the effective slider velocity, thereby preventing false positives. 
                        So if it wouldn't make a difference, it's not pointed out.
                    </note>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Before",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} {1} is snapped {2} ms before a line which would modify its slider velocity.",
                        "timestamp - ", "object", "unsnap")
                    .WithCause(
                        "A hit object is snapped 5 ms or less behind a timing line which would otherwise modify its slider velocity. " +
                        "For standard and catch this only looks at slider heads.") },

                { "After",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} {1} is snapped {2} ms after a line which would modify its slider velocity.",
                        "timestamp - ", "object", "unsnap")
                    .WithCause(
                        "Same as the other check, except after instead of before. Only applies to taiko.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (HitObject hitObject in beatmap.hitObjects)
            {
                string type =
                    hitObject is Circle   ? "Circle" :
                    hitObject is Slider   ? "Slider head" :
                    hitObject is HoldNote ? "Hold note" :
                    "Spinner";

                // SV in taiko and mania speed up all objects, whereas in catch and standard it only affects sliders
                if (!(hitObject is Slider) &&
                    beatmap.generalSettings.mode != Beatmap.Mode.Taiko &&
                    beatmap.generalSettings.mode != Beatmap.Mode.Mania)
                    continue;

                foreach (Issue issue in GetIssue(type, hitObject.time, beatmap))
                    yield return issue;
            }
        }

        /// <summary> Returns an issue if this time is very close behind to a timing line which would modify objects. </summary>
        private IEnumerable<Issue> GetIssue(string type, double time, Beatmap beatmap)
        {
            double unsnap = beatmap.GetPracticalUnsnap(time);

            TimingLine curLine = beatmap.GetTimingLine(time);
            TimingLine nextLine = beatmap.GetNextTimingLine(time);

            if (nextLine != null)
            {
                double curEffectiveBPM = curLine.svMult * beatmap.GetTimingLine<UninheritedLine>(time).bpm;
                double nextEffectiveBPM = nextLine.svMult * beatmap.GetTimingLine<UninheritedLine>(nextLine.offset).bpm;

                double deltaEffectiveBPM = curEffectiveBPM - nextEffectiveBPM;

                double timeDiff = nextLine.offset - time;
                if (timeDiff > 0 && timeDiff <= 5 &&
                    Math.Abs(unsnap) <= 1 &&
                    Math.Abs(deltaEffectiveBPM) > 1)
                {
                    yield return new Issue(GetTemplate("Before"), beatmap,
                        Timestamp.Get(time), type, $"{timeDiff:0.##}");
                }
                
                if (beatmap.generalSettings.mode == Beatmap.Mode.Taiko &&
                    timeDiff < 0 && timeDiff >= -5 &&
                    Math.Abs(unsnap) <= 1 &&
                    Math.Abs(deltaEffectiveBPM) > 1)
                {
                    yield return new Issue(GetTemplate("After"), beatmap,
                        Timestamp.Get(time), type, $"{timeDiff:0.##}");
                }
            }
        }
    }
}
