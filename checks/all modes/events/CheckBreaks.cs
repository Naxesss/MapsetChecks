using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.events
{
    [Check]
    public class CheckBreaks : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Events",
            Message = "Breaks only achievable through .osu editing.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensures that breaks work as intended."
                },
                {
                    "Reasoning",
                    @"
                    Although not visible in the editor, manually changing the break times will allow the effects of a break 
                    to happen sooner and/or later than they should. This means you may start seeing flashing arrows on the 
                    side of the screen and the background undimming while in the middle of gameplay, which only serves to 
                    make the player confused. Saving the beatmap again will fix the break times automatically.
                    <image-right>
                        https://i.imgur.com/Vh1Ha5N.png
                        An example of break effects happening in the middle of gameplay.
                    </image>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Too early or late",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} to {1} {2}. Saving the beatmap should fix this.",
                        "timestamp - ", "timestamp - ", "details")
                    .WithCause(
                        "Either the break starts less than 200 ms after the object before the end of the break, or the break ends less " +
                        "than the preemt time before the object after the start of the break.") },

                { "Too short",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} to {1} is non-functional due to being less than 650 ms.",
                        "timestamp - ", "timestamp - ")
                    .WithCause(
                        "The break is less than 650 ms in length.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (Break @break in beatmap.breaks)
            {
                // sometimes breaks are 1 ms off for some reason
                int leniency = 1;
                
                double minStart = 200;
                double minEnd = beatmap.difficultySettings.GetFadeInTime();

                double diffStart = 0;
                double diffEnd = 0;

                // checking from start of break forwards and end of break backwards ensures nothing is in between
                if (@break.time - beatmap.GetHitObject(@break.time)?.time < minStart)
                    diffStart = minStart - (@break.time - beatmap.GetHitObject(@break.time).time);

                if (beatmap.GetNextHitObject(@break.time)?.time - @break.endTime < minEnd)
                    diffEnd = minEnd - (beatmap.GetNextHitObject(@break.time).time - @break.endTime);

                if (diffStart > leniency || diffEnd > leniency)
                {
                    string issueMessage = "";

                    if (diffStart > leniency && diffEnd > leniency)
                        issueMessage = $"starts {diffStart:0.##} ms too early and ends {diffEnd:0.##} ms too late";
                    else if (diffStart > leniency)
                        issueMessage = $"starts {diffStart:0.##} ms too early";
                    else if (diffEnd > leniency)
                        issueMessage = $"ends {diffEnd:0.##} ms too late";
                    
                    yield return new Issue(GetTemplate("Too early or late"), beatmap,
                        Timestamp.Get(@break.time), Timestamp.Get(@break.endTime),
                        issueMessage);
                }

                // although this currently affects nothing, it may affect things in the future
                if (@break.endTime - @break.time < 650)
                    yield return new Issue(GetTemplate("Too short"), beatmap,
                        Timestamp.Get(@break.time), Timestamp.Get(@break.endTime));
            }
        }
    }
}
