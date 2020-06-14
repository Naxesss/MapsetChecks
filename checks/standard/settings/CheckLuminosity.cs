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

namespace MapsetChecks.checks.standard.settings
{
    [Check]
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
                    </image>
                    <br \><br \>
                    This check uses the <a href=""http://alienryderflex.com/hsp.html"">HSP colour system</a> to better approximate 
                    the way humans perceive luminosity in colours, as opposed to the HSL system where green is regarded the same 
                    luminosity as deep blue, see image.
                    <image-center>
                        https://i.imgur.com/CjPhf0b.png
                        The HSP colour system compared to the in-game HSL system.
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
                        "The HSP luminosity value of a combo colour is lower than 30. These " +
                        "values are visible in the overview section as tooltips for each colour " +
                        "if you want to check them manually.") },

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

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            // Luminosity thresholds, ~15 more lenient than the original 53 / 233
            float luminosityMinRankable = 30;
            float luminosityMinWarning = 43;
            float luminosityMax = 250;
            
            if (beatmap.colourSettings.sliderBorder != null)
            {
                Vector3 colour = beatmap.colourSettings.sliderBorder.GetValueOrDefault();
                float luminosity = GetLuminosity(colour);

                if (luminosity < luminosityMinRankable)
                    yield return new Issue(GetTemplate("Problem Border"), beatmap);
                else if (luminosity < luminosityMinWarning)
                    yield return new Issue(GetTemplate("Warning Border"), beatmap);
            }
            
            List<int> comboColoursInKiai = new List<int>();
            List<double> comboColourTime = new List<double>();
            foreach (HitObject hitObject in beatmap.hitObjects)
            {
                int combo = beatmap.GetComboColourIndex(hitObject.time);
                
                // Spinners don't have a colour.
                if (hitObject is Spinner ||
                    !beatmap.GetTimingLine(hitObject.time).kiai ||
                    comboColoursInKiai.Contains(combo))
                {
                    continue;
                }

                comboColoursInKiai.Add(combo);
                comboColourTime.Add(hitObject.time);
            }

            for (int i = 0; i < beatmap.colourSettings.combos.Count; ++i)
            {
                Vector3 colour = beatmap.colourSettings.combos.ElementAt(i);
                float luminosity = GetLuminosity(colour);

                int displayedColourIndex = beatmap.AsDisplayedComboColourIndex(i);
                
                if (luminosity < luminosityMinRankable)
                    yield return new Issue(GetTemplate("Problem Combo"), beatmap,
                        displayedColourIndex);

                else if (luminosity < luminosityMinWarning)
                    yield return new Issue(GetTemplate("Warning Combo"), beatmap,
                        displayedColourIndex);
                
                for (int j = 0; j < comboColoursInKiai.Count; ++j)
                    if (luminosity > luminosityMax && comboColoursInKiai[j] == i)
                        yield return new Issue(GetTemplate("Bright"), beatmap,
                            displayedColourIndex, Timestamp.Get(comboColourTime[j]));
            }
        }

        public float GetLuminosity(Vector3 colour)
        {
            // HSP colour model http://alienryderflex.com/hsp.html
            return
                (float)Math.Sqrt(
                    colour.X * colour.X * 0.299f +
                    colour.Y * colour.Y * 0.587f +
                    colour.Z * colour.Z * 0.114f);
        }
    }
}
