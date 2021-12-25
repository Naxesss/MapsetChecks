using System.Collections.Generic;
using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using MathNet.Numerics;

namespace MapsetChecks.Checks.AllModes.HitSounds
{
    [Check]
    public class CheckMuted : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Category = "Hit Sounds",
            Message = "Low volume hit sounding.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>
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
                    it is difficult to hear over the song, hit sounds cease to function as proper feedback.

                    Reverses are generally always done on sound cues, and assuming that's the case, it wouldn't make much sense 
                    being silent."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
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

                { "Passive Reverse",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} {1}% volume {2}, ensure there is no distinct sound here in the song.",
                        "timestamp - ", "percent", "reverse")
                    .WithCause(
                        "A slider reverse is at 10% or lower volume.") },

                { "Passive",
                    new IssueTemplate(Issue.Level.Minor,
                        "{0} {1}% volume {2}, ensure there is no distinct sound here in the song.",
                        "timestamp - ", "percent", "tick/tail")
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

                foreach (Issue issue in GetIssue(hitObject, hitObject.time, volume, isActive: true))
                    yield return issue;

                if (!(hitObject is Slider slider))
                    continue;

                for (int edgeIndex = 1; edgeIndex <= slider.edgeAmount; ++edgeIndex)
                {
                    double time = Timestamp.Round(slider.time + slider.GetCurveDuration() * edgeIndex);
                    bool isReverse = edgeIndex < slider.edgeAmount;
                    if (!isReverse)
                        // Necessary to get the exact slider end time, as opposed to a decimal value.
                        time = slider.endTime;

                    volume = GetTimingLine(beatmap, ref lineIndex, time).volume;
                    foreach (Issue issue in GetIssue(hitObject, time, volume, isActive: isReverse))
                        yield return issue;
                }

                foreach (double tickTime in slider.GetSliderTickTimes())
                {
                    volume = GetTimingLine(beatmap, ref lineIndex, tickTime).volume;
                    foreach (Issue issue in GetIssue(hitObject, tickTime, volume, isActive: false))
                        yield return issue;
                }
            }
        }

        private IEnumerable<Issue> GetIssue(HitObject hitObject, double time, float volume, bool isActive = false)
        {
            volume = GetActualVolume(volume);
            if (volume > 20)
                // Volumes greater than 20% are usually audible.
                yield break;

            bool   isHead    = time.AlmostEqual(hitObject.time);
            string timestamp = isHead ? Timestamp.Get(hitObject) : Timestamp.Get(time);
            string partName  = hitObject.GetPartName(time).ToLower().Replace("body", "tick");
            if (isActive)
            {
                if (isHead)
                {
                    if (volume <= 10)
                        yield return new Issue(GetTemplate("Warning Volume"), hitObject.beatmap,
                            timestamp, volume, partName);
                    else
                        yield return new Issue(GetTemplate("Minor Volume"), hitObject.beatmap,
                            timestamp, volume, partName);
                }
                else
                {
                    // Must be a slider reverse, mappers rarely map these to nothing.
                    if (volume <= 10)
                        yield return new Issue(GetTemplate("Passive Reverse"), hitObject.beatmap,
                            timestamp, volume, partName);
                }
            }
            else if (volume <= 10)
            {
                // Must be a slider tail or similar, these are often silenced intentionally.
                yield return new Issue(GetTemplate("Passive"), hitObject.beatmap,
                    timestamp, volume, partName);
            }
        }

        /// <summary> Returns the volume that can be heard in-game, given the timing line or object
        /// code volume from the code. Volumes less than 5% are interpreted as 5%. </summary>
        /// <param name="volume"> The volume according to the object/timing line code, in percent (i.e. 20 is 20%). </param>
        private static float GetActualVolume(float volume) => volume < 5 ? 5 : volume;

        /// <summary> Gets the timing line in effect at the given time continuing at the index given.
        /// This is more performant than <see cref="Beatmap.GetTimingLine(double, bool)"/> due to not
        /// iterating from the beginning for each hit object. </summary>
        private static TimingLine GetTimingLine(Beatmap beatmap, ref int index, double time)
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
