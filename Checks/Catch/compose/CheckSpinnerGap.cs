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
        // Allowed spinner gaps in milliseconds.
        private const int ThresholdBeforeCupSalad       = 250; // Shortest acceptable gap is 1/1 at 240 BPM.
        private const int ThresholdBeforePlatterRain    = 125; // Shortest acceptable gap is 1/2 at 240 BPM.
        private const int ThresholdBeforeOverdose       = 62;  // Shortest acceptable gap is 1/4 at 240 BPM.
        private const int ThresholdAfterCupSaladPlatter = 250;
        private const int ThresholdAfterRainOverdose    = 125;

        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
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

                    if (nextGap < ThresholdAfterCupSaladPlatter)
                        yield return new Issue(GetTemplate("SpinnerAfter"), beatmap,
                                Timestamp.Get(spinner, next), ThresholdAfterCupSaladPlatter, nextGap)
                            .ForDifficulties(Difficulty.Easy, Difficulty.Normal, Difficulty.Hard);

                    if (nextGap < ThresholdAfterRainOverdose)
                        yield return new Issue(GetTemplate("SpinnerAfter"), beatmap,
                                Timestamp.Get(spinner, next), ThresholdAfterRainOverdose, nextGap)
                            .ForDifficulties(Difficulty.Insane, Difficulty.Expert, Difficulty.Ultra);
                }

                // Check the gap before the spinner.
                if (spinner.Prev() is HitObject prev && !(prev is Spinner))
                {
                    double prevGap = spinner.time - prev.GetEndTime();

                    if (prevGap < ThresholdBeforeCupSalad)
                        yield return new Issue(GetTemplate("SpinnerBefore"), beatmap,
                                Timestamp.Get(prev, spinner), ThresholdBeforeCupSalad, prevGap)
                            .ForDifficulties(Difficulty.Easy, Difficulty.Normal);

                    if (prevGap < ThresholdBeforePlatterRain)
                        yield return new Issue(GetTemplate("SpinnerBefore"), beatmap,
                                Timestamp.Get(prev, spinner), ThresholdBeforePlatterRain, prevGap)
                            .ForDifficulties(Difficulty.Hard, Difficulty.Insane);

                    if (prevGap < ThresholdBeforeOverdose)
                        yield return new Issue(GetTemplate("SpinnerBefore"), beatmap, 
                                Timestamp.Get(prev, spinner), ThresholdBeforeOverdose, prevGap)
                            .ForDifficulties(Difficulty.Expert, Difficulty.Ultra);
                }
            }
        }
    }
}
