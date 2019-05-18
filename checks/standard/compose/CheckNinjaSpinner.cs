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

namespace MapsetChecks.checks.standard.compose
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
            foreach (HitObject hitObject in aBeatmap.hitObjects)
            {
                if (hitObject is Spinner spinner)
                {
                    double od = aBeatmap.difficultySettings.overallDifficulty;

                    double warningThreshold    = 500 + (od < 5 ? (5 - od) * -21.8 : (od - 5) * 20);  // anything above this works fine
                    double unrankableThreshold = 475 + (od < 5 ? (5 - od) * -17.5 : (od - 5) * 20);  // anything above this only works sometimes

                    if (unrankableThreshold > spinner.endTime - spinner.time)
                        yield return new Issue(GetTemplate("Unrankable"),
                            aBeatmap, Timestamp.Get(spinner));

                    else if (warningThreshold > spinner.endTime - spinner.time)
                        yield return new Issue(GetTemplate("Warning"),
                            aBeatmap, Timestamp.Get(spinner));
                }
            }
        }
    }
}
