using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace MapsetChecks.checks.timing
{
    public class CheckNinjaSpinner : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                Beatmap.Mode.Standard
            },
            Category = "Compose",
            Message = "Too short spinner.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Unrankable",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} Spinner is too short, auto cannot achieve 1000 points on this.",
                        "timestamp - ")
                    .WithCause(
                        "A spinner is predicted to, based on the OD and BPM, not be able to achieve 1000 points on this, and by a " +
                        "margin to account for any inconsistencies.") },

                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Spinner may be too short, ensure auto can achieve 1000 points on this.",
                        "timestamp - ")
                    .WithCause(
                        "Same as the other check, but without the margin, meaning the threshold is lower.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            foreach (HitObject myHitObject in aBeatmap.hitObjects)
            {
                if (myHitObject is Spinner)
                {
                    Spinner mySpinner = myHitObject as Spinner;

                    double myOD = aBeatmap.difficultySettings.overallDifficulty;

                    double myWarningThreshold    = 500 + (myOD < 5 ? (5 - myOD) * -21.8 : (myOD - 5) * 20);  // anything above this works fine
                    double myUnrankableThreshold = 475 + (myOD < 5 ? (5 - myOD) * -17.5 : (myOD - 5) * 20);  // anything above this only works sometimes

                    if (myUnrankableThreshold > mySpinner.endTime - mySpinner.time)
                        yield return new Issue(GetTemplate("Unrankable"),
                            aBeatmap, Timestamp.Get(mySpinner));

                    else if (myWarningThreshold > mySpinner.endTime - mySpinner.time)
                        yield return new Issue(GetTemplate("Warning"),
                            aBeatmap, Timestamp.Get(mySpinner));
                }
            }
        }
    }
}
