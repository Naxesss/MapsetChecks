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
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Default",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "Default combo colours without forced skin.")
                    .WithCause(
                        "A beatmap has no custom combo colours and does not force the default skin.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            if (aBeatmap.generalSettings.skinPreference != "Default" && !aBeatmap.colourSettings.combos.Any())
                yield return new Issue(GetTemplate("Default"), aBeatmap);
        }
    }
}
