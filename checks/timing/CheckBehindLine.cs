using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MapsetChecks.checks.timing
{
    public class CheckBehindLine : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Timing",
            Message = "Hit object is slightly behind a line which would modify it.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Behind",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} {1} is snapped {2} ms behind a line which would modify its slider velocity.",
                        "timestamp - ", "object", "unsnap")
                    .WithCause(
                        "A hit object is snapped 5 ms or less behind a timing line which would otherwise modify its slider velocity. " +
                        "For standard and catch this only looks at slider heads.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            foreach (HitObject hitObject in aBeatmap.hitObjects)
            {
                string type =
                    hitObject is Circle   ? "Circle" :
                    hitObject is Slider   ? "Slider head" :
                    hitObject is HoldNote ? "Hold note" :
                    "Spinner";

                // SV in taiko and mania speed up all objects, whereas in catch and standard it only affects sliders
                if (hitObject is Slider ||
                    aBeatmap.generalSettings.mode == Beatmap.Mode.Taiko ||
                    aBeatmap.generalSettings.mode == Beatmap.Mode.Mania)
                {
                    foreach (Issue issue in GetIssue(type, hitObject.time, aBeatmap, hitObject))
                        yield return issue;
                }
            }
        }

        /// <summary> Returns an issue if this time is very close behind to a timing line which would modify objects. </summary>
        private IEnumerable<Issue> GetIssue<T>(string aType, double aTime, Beatmap aBeatmap, params T[] anObject)
        {
            double? unsnapIssue = aBeatmap.GetUnsnapIssue(aTime);

            double unsnap = aBeatmap.GetPracticalUnsnap(aTime);
            double roundedUnsnap = Math.Round(unsnap * 1000) / 1000;

            TimingLine curLine = aBeatmap.GetTimingLine(aTime);
            TimingLine nextLine = aBeatmap.GetNextTimingLine(aTime);

            if (nextLine != null)
            {
                double curEffectiveBPM = curLine.svMult * (aBeatmap.GetTimingLine(aTime, true) as UninheritedLine).bpm;
                double nextEffectiveBPM = nextLine.svMult * (aBeatmap.GetTimingLine(nextLine.offset, true) as UninheritedLine).bpm;

                double deltaEffectiveBPM = curEffectiveBPM - nextEffectiveBPM;

                double myTimeDiff = nextLine.offset - aTime;
                if (myTimeDiff > 0 && myTimeDiff <= 5 &&
                    Math.Abs(unsnap) <= 1 &&
                    deltaEffectiveBPM > 1)
                {
                    yield return new Issue(GetTemplate("Behind"), aBeatmap,
                        Timestamp.Get(aTime), aType, myTimeDiff.ToString(CultureInfo.InvariantCulture));
                }
            }
        }
    }
}
