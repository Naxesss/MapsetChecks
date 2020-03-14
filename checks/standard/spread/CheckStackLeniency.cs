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
    public class CheckStackLeniency : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                Beatmap.Mode.Standard
            },
            Difficulties = new Beatmap.Difficulty[]
            {
                Beatmap.Difficulty.Easy,
                Beatmap.Difficulty.Normal,
                Beatmap.Difficulty.Hard,
                Beatmap.Difficulty.Insane,
            },
            Category = "Spread",
            Message = "Perfect stacks too close in time.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing objects from perfectly, or almost perfectly, overlapping when close in time for easy to insane difficulties."
                },
                {
                    "Reasoning",
                    @"
                    Objects stacked perfectly on top of each other close in time is read almost ambigiously to a single object, even for moderately 
                    experienced players. The lower in difficulty you get, the more beneficial it is to simply use a regular stack or overlap instead
                    as trivializing readability gets more important."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Problem",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} Stack leniency should be at least {1}.",
                        "timestamp - ", "stack leniency")
                    .WithCause(
                        "Two objects are overlapping perfectly and are less than 1/1, 1/1, 1/2, or 1/4 apart (assuming 160 BPM), for E/N/H/I respectively.") },

                { "Problem Failed Stack",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} Failed stack, objects are {1} px apart, which is basically a perfect stack.",
                        "timestamp - ", "gap")
                    .WithCause(
                        "Same as the other check, except applies to non-stacked objects within 2 px of one another.") },

                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Stack leniency should be at least {1}.",
                        "timestamp - ", "stack leniency")
                    .WithCause(
                        "Same as the other check, except only appears for insane difficulties, as this becomes a guideline.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            double[] snapping = new double[] { 1, 1, 0.5, 0.25 };

            for (int diffIndex = 0; diffIndex < snapping.Length; ++diffIndex)
            {
                double timeGap = snapping[diffIndex] * 60000 / 160d;

                int hitObjectCount = beatmap.hitObjects.Count();
                for (int i = 0; i < hitObjectCount - 1; ++i)
                {
                    for (int j = i + 1; j < hitObjectCount; ++j)
                    {
                        HitObject hitObject = beatmap.hitObjects[i];
                        HitObject otherHitObject = beatmap.hitObjects[j];

                        if (hitObject is Spinner || otherHitObject is Spinner)
                            break;

                        // Hit objects are sorted by time, so difference in time will only increase.
                        if (otherHitObject.time - hitObject.time >= timeGap)
                            break;

                        if (hitObject.Position == otherHitObject.Position)
                        {
                            int requiredStackLeniency =
                                (int)Math.Ceiling((otherHitObject.time - hitObject.time) /
                                    (beatmap.difficultySettings.GetFadeInTime() * 0.1));

                            string template = diffIndex >= (int)Beatmap.Difficulty.Insane ? "Warning" : "Problem";

                            yield return new Issue(GetTemplate(template), beatmap,
                                Timestamp.Get(hitObject, otherHitObject), requiredStackLeniency)
                                .ForDifficulties((Beatmap.Difficulty)diffIndex);
                        }
                        else
                        {
                            // Unstacked objects within 2 px of one another are considered failed stacks.
                            double distance = (hitObject.Position - otherHitObject.Position).Length();
                            if (distance > 2)
                                continue;

                            yield return new Issue(GetTemplate("Problem Failed Stack"), beatmap,
                                Timestamp.Get(hitObject, otherHitObject), $"{distance:0.##}")
                                .ForDifficulties((Beatmap.Difficulty)diffIndex);
                        }
                    }
                }
            }
        }
    }
}
