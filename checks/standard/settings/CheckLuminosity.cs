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

namespace MapsetChecks.checks.standard.settings
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
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing combo colours from blending into dimmed backgrounds or flashing too intensely in kiai."
                },
                {
                    "Reasoning",
                    @"
                    Although objects by default have a white border around them making them visible, the approach circles are 
                    affected by combo colour and will become impossible to see with colour 0,0,0. Stripping the game of 
                    important gameplay indicators or generally messing with them (see check for modified breaks) is not 
                    something beatmaps are expected to do, as they need to be consistent to work properly.
                    <image-right>
                        https://i.imgur.com/wxoMMQG.png
                        A slider whose approach circle is only visible on its borders and path, due to the rest blending into 
                        the dimmed bg.
                    </image>
                    <br \><br \>
                    As for bright colours, when outside of kiai they're fine, but while in kiai the game flashes them, 
                    attempting to make them even brighter without caring about them already being really bright, resulting in 
                    pretty strange behaviour for some monitors and generally just unpleasant contrast.
                    <image-right>
                        https://i.imgur.com/9cRTvJc.png
                        An example of a slider with colour 255,255,255 while in the middle of flashing.
                    </image>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Problem Combo",
                    new IssueTemplate(Issue.Level.Problem,
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

                { "Problem Border",
                    new IssueTemplate(Issue.Level.Problem,
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
                    yield return new Issue(GetTemplate("Problem Border"), aBeatmap);

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
                    yield return new Issue(GetTemplate("Problem Combo"), aBeatmap,
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
