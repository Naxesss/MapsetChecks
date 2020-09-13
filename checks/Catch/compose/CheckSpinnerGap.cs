using System.Collections.Generic;
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
            HitObject lastObject = null;
            foreach (HitObject hitObject in beatmap.hitObjects)
            {
                if (lastObject is Spinner && hitObject is Spinner)
                    continue;

                // Check if the previous object was a spinner so we can determine the 'after' gap.
                if (lastObject is Spinner)
                {
                    double timeBetweenSpinnerAndNextObject = hitObject.GetPrevDeltaTime();

                    if (timeBetweenSpinnerAndNextObject < ThresholdAfterCupSaladPlatter)
                        yield return new Issue(GetTemplate("SpinnerAfter"), beatmap,
                                Timestamp.Get(lastObject, hitObject), ThresholdAfterCupSaladPlatter, timeBetweenSpinnerAndNextObject)
                            .ForDifficulties(Difficulty.Easy, Difficulty.Normal, Difficulty.Hard);

                    if (timeBetweenSpinnerAndNextObject < ThresholdAfterRainOverdose)
                        yield return new Issue(GetTemplate("SpinnerAfter"), beatmap,
                                Timestamp.Get(lastObject, hitObject), ThresholdAfterRainOverdose, timeBetweenSpinnerAndNextObject)
                            .ForDifficulties(Difficulty.Insane, Difficulty.Expert, Difficulty.Ultra);
                }

                if (hitObject is Spinner)
                {
                    double timeBetweenPreviousObjectAndSpinner = hitObject.GetPrevDeltaTime();

                    if (timeBetweenPreviousObjectAndSpinner < ThresholdBeforeCupSalad)
                        yield return new Issue(GetTemplate("SpinnerBefore"), beatmap,
                                Timestamp.Get(lastObject, hitObject), ThresholdBeforeCupSalad, timeBetweenPreviousObjectAndSpinner)
                            .ForDifficulties(Difficulty.Easy, Difficulty.Normal);

                    if (timeBetweenPreviousObjectAndSpinner < ThresholdBeforePlatterRain)
                        yield return new Issue(GetTemplate("SpinnerBefore"), beatmap,
                                Timestamp.Get(lastObject, hitObject), ThresholdBeforePlatterRain, timeBetweenPreviousObjectAndSpinner)
                            .ForDifficulties(Difficulty.Hard, Difficulty.Insane);

                    if (timeBetweenPreviousObjectAndSpinner < ThresholdBeforeOverdose)
                        yield return new Issue(GetTemplate("SpinnerBefore"), beatmap, 
                                Timestamp.Get(lastObject, hitObject), ThresholdBeforeOverdose, timeBetweenPreviousObjectAndSpinner)
                            .ForDifficulties(Difficulty.Expert, Difficulty.Ultra);
                }
                
                // Specify the last object so we can use it to determine the 'after' gap if needed.
                lastObject = hitObject;
            }
        }
    }
}
