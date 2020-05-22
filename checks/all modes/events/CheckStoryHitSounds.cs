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
    public class CheckStoryHitSounds : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                Beatmap.Mode.Standard,
                Beatmap.Mode.Taiko,
                Beatmap.Mode.Catch
            },
            Category = "Events",
            Message = "Storyboarded hit sounds.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing storyboard sounds from replacing or becoming ambigious with any beatmap hit sounds."
                },
                {
                    "Reasoning",
                    @"
                    Storyboarded hit sounds always play at the same time regardless of however late or early the player clicks 
                    on an object, meaning they do not provide proper active hit object feedback, unlike regular hit sounds. This 
                    contradicts the purpose of hit sounds and is likely to be confusing for players if similar samples as the 
                    hit sounds are used.
                    <note>
                        Mania is exempt from this due to multiple objects at the same point in time being possible, leading 
                        to regular hit sounding working poorly, for example amplifying the volume if concurrent objects have 
                        the same hit sounds.
                    </note>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Storyboarded Hit Sound",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Storyboarded hit sound ({1}, {2}%) from {3} file.",
                        "timestamp - ", "path", "volume", ".osu/.osb")
                    .WithCause(
                        "The .osu file or .osb file contains storyboarded hit sounds.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (Beatmap beatmap in beatmapSet.beatmaps)
            {
                foreach (Sample storyHitSound in beatmap.samples)
                    foreach (Issue issue in GetStoryHitSoundIssue(beatmap, storyHitSound, ".osu"))
                        yield return issue;

                if (beatmapSet.osb == null)
                    continue;

                foreach (Sample storyHitSound in beatmapSet.osb.samples)
                    foreach (Issue issue in GetStoryHitSoundIssue(beatmap, storyHitSound, ".osb"))
                        yield return issue;
            }
        }

        private IEnumerable<Issue> GetStoryHitSoundIssue(Beatmap beatmap, Sample sample, string origin)
        {
            yield return new Issue(GetTemplate("Storyboarded Hit Sound"), beatmap,
                Timestamp.Get(sample.time),
                sample.path, sample.volume,
                origin);
        }
    }
}
