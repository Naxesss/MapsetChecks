using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System.Collections.Generic;
using System.Linq;

namespace MapsetChecks.checks.general.audio
{
    public class CheckHitSoundFormat : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Audio",
            Message = "Incorrect hit sound format.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "ogg",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "\"{0}\" The .ogg format is deprecated and is no longer allowed.",
                        "path")
                    .WithCause(
                        "A hit sound file is using the .ogg format.") },

                { "mp3",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "\"{0}\" This .mp3 file is used for active hit sounding, see {1}.",
                        "path", "timestamp - ")
                    .WithCause(
                        "A hit sound file is using the .mp3 format.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            if (aBeatmapSet.hitSoundFiles != null)
            {
                foreach (string hitSoundFile in aBeatmapSet.hitSoundFiles)
                {
                    if (hitSoundFile.EndsWith(".ogg"))
                        yield return new Issue(GetTemplate("ogg"), null,
                            hitSoundFile);
                    
                    // The .mp3 format includes inherent delays and are as such not fit for active hit sounding.
                    if (hitSoundFile.EndsWith(".mp3"))
                    {
                        foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
                        {
                            foreach (HitObject hitObject in beatmap.hitObjects)
                            {
                                if (hitObject is Spinner)
                                    continue;

                                // Only the hit sound edge at which the object is clicked is considered active.
                                if (hitObject.GetUsedHitSamples().Any(aSample =>
                                        aSample.time == hitObject.time &&
                                        aSample.hitSource == HitSample.HitSource.Edge &&
                                        aSample.GetFileName() == hitSoundFile))
                                {
                                    yield return new Issue(GetTemplate("mp3"), null,
                                        hitSoundFile, Timestamp.Get(hitObject));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
