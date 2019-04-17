using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System.Collections.Generic;

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
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" Ensure the .mp3 format is not being used for active hit sounding.",
                        "path")
                    .WithCause(
                        "A hit sound file is using the .mp3 format.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            if (beatmapSet.hitsoundFiles != null)
            {
                foreach (string hitsoundFile in beatmapSet.hitsoundFiles)
                {
                    if (hitsoundFile.EndsWith(".ogg"))
                        yield return new Issue(GetTemplate("ogg"), null,
                            hitsoundFile);

                    // needs to be passive, can't be active
                    if (hitsoundFile.EndsWith(".mp3"))
                    {
                        foreach (Beatmap beatmap in beatmapSet.beatmaps)
                        {
                            foreach (HitObject hitObject in beatmap.hitObjects)
                            {
                                if (hitObject is Spinner)
                                    continue;

                                if (beatmap.GetTimingLine(hitObject.time).sampleset == Beatmap.Sampleset.Soft)
                                {

                                }
                            }
                        }

                        yield return new Issue(GetTemplate("mp3"), null,
                            hitsoundFile);
                    }
                }
            }
        }
    }
}
