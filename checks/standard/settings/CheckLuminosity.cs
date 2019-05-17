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
    public class CheckLuminosity : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                Beatmap.Mode.Standard
            },
            Category = "Settings",
            Message = "Too dark or bright combo colours or slider borders.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Unrankable Combo",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "Combo colour {0} is way too dark.",
                        "number")
                    .WithCause(
                        "The HSL luminosity value of a combo colour is lower than 30.") },

                { "Warning Combo",
                    new IssueTemplate(Issue.Level.Warning,
                        "Combo colour {0} is really dark.",
                        "number")
                    .WithCause(
                        "Same as the first check, but lower than 43 instead.") },

                { "Unrankable Border",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "Slider border is way too dark.")
                    .WithCause(
                        "Same as the first check, except applies on the slider border instead.") },

                { "Warning Border",
                    new IssueTemplate(Issue.Level.Warning,
                        "Slider border is really dark.")
                    .WithCause(
                        "Same as the second check, except applies on the slider border instead.") },

                { "Bright",
                    new IssueTemplate(Issue.Level.Warning,
                        "Combo colour {0} is really bright in kiai sections, see {1}.",
                        "number", "example object")
                    .WithCause(
                        "Same as the first check, but higher than 250 and requires that at least one hit object with the combo is in a kiai section.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            // Luminosity thresholds, ~15 more lenient than the original 53 / 233
            float luminosityMinRankable = 30;
            float luminosityMinWarning = 43;
            float luminosityMax = 250;
            
            if (aBeatmap.colourSettings.sliderBorder != null)
            {
                Vector3 colour = aBeatmap.colourSettings.sliderBorder.GetValueOrDefault();
                float luminosity = GetLuminosity(colour);

                if (luminosity < luminosityMinRankable)
                    yield return new Issue(GetTemplate("Unrankable Border"), aBeatmap);

                if (luminosity < luminosityMinWarning)
                    yield return new Issue(GetTemplate("Warning Border"), aBeatmap);
            }
            
            List<int> comboColoursInKiai = new List<int>();
            List<double> comboColourTime = new List<double>();
            foreach (HitObject hitObject in aBeatmap.hitObjects)
            {
                int combo = aBeatmap.GetComboColourIndex(hitObject.time);
                
                // Spinners don't have a colour.
                if (!(hitObject is Spinner) &&
                    aBeatmap.GetTimingLine(hitObject.time).kiai &&
                    !comboColoursInKiai.Contains(combo))
                {
                    comboColoursInKiai.Add(combo);
                    comboColourTime.Add(hitObject.time);
                }
            }

            for (int i = 0; i < aBeatmap.colourSettings.combos.Count; ++i)
            {
                Vector3 colour = aBeatmap.colourSettings.combos.ElementAt(i);
                float luminosity = GetLuminosity(colour);

                int displayedColourIndex = aBeatmap.GetDisplayedComboColourIndex(i);
                
                if (luminosity < luminosityMinRankable)
                    yield return new Issue(GetTemplate("Unrankable Combo"), aBeatmap,
                        displayedColourIndex);

                else if (luminosity < luminosityMinWarning)
                    yield return new Issue(GetTemplate("Warning Combo"), aBeatmap,
                        displayedColourIndex);
                
                for (int j = 0; j < comboColoursInKiai.Count; ++j)
                    if (luminosity > luminosityMax && comboColoursInKiai[j] == i)
                        yield return new Issue(GetTemplate("Bright"), aBeatmap,
                            displayedColourIndex, Timestamp.Get(comboColourTime[j]));
            }
        }

        public float GetLuminosity(Vector3 aColour)
        {
            // HSP colour model http://alienryderflex.com/hsp.html
            return
                (float)Math.Sqrt(
                    aColour.X * aColour.X * 0.299f +
                    aColour.Y * aColour.Y * 0.587f +
                    aColour.Z * aColour.Z * 0.114f);
        }
    }
}
