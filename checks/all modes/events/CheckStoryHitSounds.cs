using MapsetChecks.objects;
using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.events
{
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
            Author = "Naxess"
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

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
            {
                foreach (StoryHitSound storyHitSound in beatmap.storyHitSounds)
                    foreach (Issue issue in GetStoryHitSoundIssue(beatmap, storyHitSound, ".osu"))
                        yield return issue;

                if (aBeatmapSet.osb != null)
                    foreach (StoryHitSound storyHitSound in aBeatmapSet.osb.storyHitSounds)
                        foreach (Issue issue in GetStoryHitSoundIssue(beatmap, storyHitSound, ".osb"))
                            yield return issue;
            }
        }

        private IEnumerable<Issue> GetStoryHitSoundIssue(Beatmap aBeatmap, StoryHitSound aStoryHitSound, string anOrigin)
        {
            yield return new Issue(GetTemplate("Storyboarded Hit Sound"), aBeatmap,
                Timestamp.Get(aStoryHitSound.time),
                aStoryHitSound.path, aStoryHitSound.volume,
                anOrigin);
        }
    }
}
