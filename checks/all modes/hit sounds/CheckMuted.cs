using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.hit_sounds
{
    [Check]
    public class CheckMuted : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Hit Sounds",
            Message = "Low volume hit sounding.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring that active hit object feedback is audible."
                },
                {
                    "Reasoning",
                    @"
                    All active hit objects (i.e. circles, slider heads, and starts of hold notes) should provide some feedback 
                    so that players can hear if they're clicking too early or late. By reducing the volume to the point where 
                    it is difficult to hear over the song, hit sounds cease to function as proper feedback."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Warning Volume",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} {1}% volume {2}, this may be hard to hear over the song.",
                        "timestamp - ", "percent", "active hit object")
                    .WithCause(
                        "An active hit object is at 10% or lower volume.") },

                { "Minor Volume",
                    new IssueTemplate(Issue.Level.Minor,
                        "{0} {1}% volume {2}, this may be hard to hear over the song.",
                        "timestamp - ", "percent", "active hit object")
                    .WithCause(
                        "An active hit object is at 20% or lower volume.") },

                { "Passive",
                    new IssueTemplate(Issue.Level.Minor,
                        "{0} {1}% volume {2}, ensure there is no distinct sound here in the song.",
                        "timestamp - ", "percent", "tick/reverse/tail")
                    .WithCause(
                        "A passive hit object is at 10% or lower volume.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            int lineIndex = 0;
            foreach (HitObject hitObject in beatmap.hitObjects)
            {
                if (!(hitObject is Circle || hitObject is Slider || hitObject is HoldNote))
                    continue;

                // Object-specific volume overrides line-specific volume for circles and hold notes
                // (feature for Mania hit sounding) when it is > 0. However, this applies to other modes as well.
                float volume =
                    !(hitObject is Slider) && hitObject.volume > 0 && hitObject.volume != null ?
                        hitObject.volume.GetValueOrDefault() :
                        GetTimingLine(beatmap, ref lineIndex, hitObject.time).volume;

                // < 5% is interpreted as 5%
                if (volume < 5)
                    volume = 5;
                    
                if (volume <= 10)
                    yield return new Issue(GetTemplate("Warning Volume"), beatmap,
                        Timestamp.Get(hitObject), volume, hitObject.GetPartName(hitObject.time).ToLower());

                else if (volume <= 20)
                    yield return new Issue(GetTemplate("Minor Volume"), beatmap,
                        Timestamp.Get(hitObject), volume, hitObject.GetPartName(hitObject.time).ToLower());

                if (!(hitObject is Slider slider))
                    continue;

                for (int edgeIndex = 0; edgeIndex < slider.edgeAmount; ++edgeIndex)
                {
                    double time = Timestamp.Round(slider.time + slider.GetCurveDuration() * edgeIndex);

                    if (edgeIndex == slider.edgeAmount - 1)
                        time = slider.endTime;

                    volume = GetTimingLine(beatmap, ref lineIndex, hitObject.time).volume;
                    if (volume <= 10)
                        yield return new Issue(GetTemplate("Passive"), beatmap,
                            Timestamp.Get(time), volume, hitObject.GetPartName(time).ToLower());
                }
            }
        }

        /// <summary> Gets the timing line in effect at the given time continuing at the index given.
        /// This is more performant than <see cref="Beatmap.GetTimingLine(double, bool)"/> due to not
        /// iterating from the beginning for each hit object. </summary>
        private TimingLine GetTimingLine(Beatmap beatmap, ref int index, double time)
        {
            int length = beatmap.timingLines.Count;
            for (; index < length; ++index)
                // Uses the 5 ms hit sound leniency.
                if (index > 0 && beatmap.timingLines[index].offset >= time + 5)
                    return beatmap.timingLines[index - 1];

            return beatmap.timingLines[length - 1];
        }
    }
}
