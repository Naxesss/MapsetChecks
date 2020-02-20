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
    public class CheckDefaultColours : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                Beatmap.Mode.Standard
            },
            Category = "Settings",
            Message = "Default combo colours without forced skin.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing the combo colours chosen without additional input from blending into the background.
                    <image-right>
                        https://i.imgur.com/G5vTU7f.png
                        The combo colour section in song setup without custom colours ticked.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    If you leave the combo colour setting as it is when you create a beatmap, no [Colours] section will 
                    be created in the .osu, meaning the skins of users will override them. Since we can't control which 
                    colours they may use or force them to dim the background, the colours may blend into the background 
                    making for an unfair gameplay experience.
                    <br \><br \>
                    If you set a preferred skin in the beatmap however, for example default, that skin will be used over 
                    any user skin, but many players switch skins to get away from default, so would not recommend this.
                    If you want the default colours, simply tick the ""Enable Custom Colours"" checkbox instead."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Default",
                    new IssueTemplate(Issue.Level.Problem,
                        "Default combo colours without preferred skin.")
                    .WithCause(
                        "A beatmap has no custom combo colours and does not have any preferred skin.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            if (beatmap.generalSettings.skinPreference != "Default" && !beatmap.colourSettings.combos.Any())
                yield return new Issue(GetTemplate("Default"), beatmap);
        }
    }
}
