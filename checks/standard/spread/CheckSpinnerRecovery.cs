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
using System.Numerics;

namespace MapsetChecks.checks.standard.spread
{
    [Check]
    public class CheckSpinnerRecovery : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                Beatmap.Mode.Standard
            },
            Category = "Spread",
            Message = "Too short spinner time or spinner recovery time.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing spinners or objects following them from being too fast to react to."
                },
                {
                    "Reasoning",
                    @"
                    Newer players need time to recognize that they should begin spinning, as well as time to regain control of their 
                    cursor after spinning. Hence, too short spinners will often lose them accuracy or combo, and objects very close 
                    to the end of spinners will probably be missed or clicked on in panic before the spinner has ended."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Problem Length",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} Spinner length is too short ({1} ms, expected {2}).",
                        "timestamp - ", "duration", "duration")
                    .WithCause(
                        "A spinner is shorter than 4, 3 or 2 beats for Easy, Normal and Hard respectively, assuming 240 bpm.") },

                { "Warning Length",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Spinner length is probably too short ({1} ms, expected {2}).",
                        "timestamp - ", "duration", "duration")
                    .WithCause(
                        "Same as the first check, except 20% more lenient, implying that 200 bpm is assumed instead.") },

                { "Problem Recovery",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} Spinner recovery time is too short ({1} ms, expected {2}).",
                        "timestamp - ", "duration", "duration")
                    .WithCause(
                        "The time after a spinner ends to the next object is shorter than 4, 3 or 2 beats for Easy, Normal and Hard respectively, " +
                        "assuming 240 bpm, where both the non-scaled and bpm-scaled thresholds must be exceeded.") },

                { "Warning Recovery",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Spinner recovery time is probably too short ({1} ms, expected {2}).",
                        "timestamp - ", "duration", "duration")
                    .WithCause(
                        "Same as the other recovery check, except 20% more lenient, implying that 200 bpm is assumed instead.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (HitObject hitObject in beatmap.hitObjects)
            {
                if (!(hitObject is Spinner spinner))
                    continue;

                foreach (Issue issue in GetLengthIssues(beatmap, spinner))
                    yield return issue;

                foreach (Issue issue in GetRecoveryIssues(beatmap, spinner))
                    yield return issue;
            }
        }

        // Equal to the ms length of a beat in 180 BPM divided by the same thing in 240 BPM.
        // So when multiplied by the expected time, we get the time that the Ranking Criteria wanted, which is based on 180 BPM.
        private readonly double expectedMultiplier = 4 / 3d;

        private IEnumerable<Issue> GetLengthIssues(Beatmap beatmap, Spinner spinner)
        {
            double spinnerTime = spinner.endTime - spinner.time;
            double[] spinnerTimeExpected = new double[] { 1000, 750, 500 }; // 4, 3 and 2 beats respectively, 240 bpm

            for (int diffIndex = 0; diffIndex < spinnerTimeExpected.Length; ++diffIndex)
            {
                double expectedLength = Math.Ceiling(spinnerTimeExpected[diffIndex] * expectedMultiplier);

                double problemThreshold = spinnerTimeExpected[diffIndex];
                double warningThreshold = spinnerTimeExpected[diffIndex] * 1.2; // same thing but 200 bpm instead

                if (spinnerTime < problemThreshold)
                    yield return new Issue(GetTemplate("Problem Length"), beatmap,
                        Timestamp.Get(spinner), spinnerTime, expectedLength)
                        .ForDifficulties((Beatmap.Difficulty)diffIndex);

                else if (spinnerTime < warningThreshold)
                    yield return new Issue(GetTemplate("Warning Length"), beatmap,
                        Timestamp.Get(spinner), spinnerTime, expectedLength)
                        .ForDifficulties((Beatmap.Difficulty)diffIndex);
            }
        }

        private IEnumerable<Issue> GetRecoveryIssues(Beatmap beatmap, Spinner spinner)
        {
            HitObject nextObject = beatmap.GetNextHitObject(spinner.time);

            // Do not check time between two spinners since all you'd need to do is keep spinning.
            if (nextObject != null && !(nextObject is Spinner))
            {
                double recoveryTime = nextObject.time - spinner.endTime;

                UninheritedLine line = beatmap.GetTimingLine<UninheritedLine>(nextObject.time);
                double bpmScaling = GetScaledTiming(line.bpm);
                double recoveryTimeScaled = recoveryTime / bpmScaling;

                double[] recoveryTimeExpected = new double[] { 1000, 500, 250 }; // 4, 2 and 1 beats respectively, 240 bpm

                // Tries both scaled and regular recoveries, and only if both are exceeded does it create an issue.
                for (int diffIndex = 0; diffIndex < recoveryTimeExpected.Length; ++diffIndex)
                {
                    // Picks whichever is greatest of the scaled and regular versions.
                    double expectedScaledMultiplier = (bpmScaling < 1 ? bpmScaling : 1);
                    double expectedRecovery = Math.Ceiling(recoveryTimeExpected[diffIndex] * expectedScaledMultiplier * expectedMultiplier);

                    double problemThreshold = recoveryTimeExpected[diffIndex];
                    double warningThreshold = recoveryTimeExpected[diffIndex] * 1.2;

                    if (recoveryTimeScaled < problemThreshold && recoveryTime < problemThreshold)
                        yield return new Issue(GetTemplate("Problem Recovery"), beatmap,
                            Timestamp.Get(spinner, nextObject), recoveryTime, expectedRecovery)
                            .ForDifficulties((Beatmap.Difficulty)diffIndex);

                    else if (recoveryTimeScaled < warningThreshold && recoveryTime < warningThreshold)
                        yield return new Issue(GetTemplate("Warning Recovery"), beatmap,
                            Timestamp.Get(spinner, nextObject), recoveryTime, expectedRecovery)
                            .ForDifficulties((Beatmap.Difficulty)diffIndex);
                }
            }
        }

        /// <summary> Scales the bpm in accordance to https://osu.ppy.sh/help/wiki/Ranking_Criteria/osu!/Scaling_BPM,
        /// where 180 bpm is 1, 120 bpm is 0.5, and 240 bpm is 2. </summary>
        private double GetScaledTiming(double bpm) => Math.Pow(bpm, 2) / 14400 - bpm / 80 + 1;
    }
}
