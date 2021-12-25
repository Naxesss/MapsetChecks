using System.Collections.Generic;
using System.Linq;
using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using static MapsetParser.objects.Beatmap;

namespace MapsetChecks.checks.Catch.compose
{
    [Check]
    public class CheckSpinnerGap : BeatmapCheck
    {
        // Allowed spinner gaps in milliseconds.          Cup  Salad  Platter  Rain  Overdose, Overdose+
        private static readonly int[] ThresholdBefore = { 250, 250,   125,     125,  62,       62        };
        private static readonly int[] ThresholdAfter  = { 250, 250,   250,     125,  125,      125       };

        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Modes = new[] { Mode.Catch },
            Category = "Compose",
            Message = "Spinner gap too small.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    There must be a gap between the start and end of a spinner to ensure readability."
                },
                {
                    "Reasoning",
                    @"
                    Spinners can make it difficult to read when provided shortly before/after an object. On lower difficulties 
                    the approach rate is slower and will result in a more clustered experience. The spinner gap is essential 
                    to give the player enough time to react to the next object and to avoid spinner traps."
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "SpinnerBefore",
                    new IssueTemplate(Issue.Level.Problem,
                            "{0} The spinner must be at least {1} ms apart from the previous object, currently {2} ms.",
                            "timestamp - ", "required duration", "current duration")
                        .WithCause(
                            "The spinner starts too early.") },
                { "SpinnerAfter",
                    new IssueTemplate(Issue.Level.Problem,
                            "{0} The spinner must be at least {1} ms apart from the next object, currently {2} ms.",
                            "timestamp - ", "required duration", "current duration")
                        .WithCause(
                            "The spinner ends too late.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (Spinner spinner in beatmap.hitObjects.OfType<Spinner>())
            {
                // Check the gap after the spinner.
                if (spinner.Next() is HitObject next && !(next is Spinner))
                {
                    double nextGap = next.time - spinner.endTime;

                    for (int diffIndex = 0; diffIndex < (int)Difficulty.Ultra; ++diffIndex)
                        if (nextGap < ThresholdAfter[diffIndex])
                            yield return new Issue(GetTemplate("SpinnerAfter"), beatmap,
                                    Timestamp.Get(spinner, next), ThresholdAfter[diffIndex], nextGap)
                                .ForDifficulties((Difficulty)diffIndex);
                }

                // Check the gap before the spinner.
                if (spinner.Prev() is HitObject prev && !(prev is Spinner))
                {
                    double prevGap = spinner.time - prev.GetEndTime();

                    for (int diffIndex = 0; diffIndex < (int)Difficulty.Ultra; ++diffIndex)
                        if (prevGap < ThresholdBefore[diffIndex])
                            yield return new Issue(GetTemplate("SpinnerBefore"), beatmap,
                                    Timestamp.Get(prev, spinner), ThresholdBefore[diffIndex], prevGap)
                                .ForDifficulties((Difficulty)diffIndex);
                }
            }
        }
    }
}
