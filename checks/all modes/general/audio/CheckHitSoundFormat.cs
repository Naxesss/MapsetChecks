using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using MapsetVerifierFramework.objects.resources;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.general.audio
{
    [Check]
    public class CheckHitSoundFormat : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Audio",
            Message = "Incorrect hit sound format.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Prevents deprecated file formats for hit sounds, as well as discourages potentially detrimental ones.
                    <image-right>
                        https://i.imgur.com/yAF6pEq.png
                        One of the hit sound files being an mp3.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    The ogg format is no longer supported and the mp3 format often includes inherent delays. As 
                    such, the wav format is preferred for any hit sound file.
                    <note>
                        Passive objects such as slider tails are not clicked and as such do not need accurate 
                        feedback and may use the mp3 format because of this.
                    </note>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "ogg",
                    new IssueTemplate(Issue.Level.Problem,
                        "\"{0}\" is using the OGG format, which is deprecated and no longer allowed.",
                        "path")
                    .WithCause(
                        "A hit sound file is using the .ogg format.") },

                { "mp3",
                    new IssueTemplate(Issue.Level.Problem,
                        "\"{0}\" is using the MP3 format and is used for active hit sounding, see {1} in {2} for example.",
                        "path", "timestamp - ", "beatmap")
                    .WithCause(
                        "A hit sound file is using the .mp3 format.") },

                { "Unexpected Format",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" is using an unexpected format: \"{1}\".",
                        "path", "actual format")
                    .WithCause(
                        "A hit sound file is using a format which is neither OGG, Wave, or MP3.") },

                { "Incorrect Extension",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" is using the {1} format, but doesn't use the .wav extension.",
                        "path", "actual format")
                    .WithCause(
                        "A hit sound file is using an incorrect extension.") },

                { "Exception",
                    new IssueTemplate(Issue.Level.Error,
                        "\"{0}\" returned exception \"{1}\", so unable to check that.",
                        "path", "exception")
                    .WithCause(
                        "An error occurred trying to check the format of a hit sound file.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            if (aBeatmapSet.hitSoundFiles != null)
            {
                foreach (string hitSoundFile in aBeatmapSet.hitSoundFiles)
                {
                    string fullPath = Path.Combine(aBeatmapSet.songPath, hitSoundFile);

                    ManagedBass.ChannelType actualFormat = 0;
                    Exception exception = null;
                    try
                    { actualFormat = Audio.GetFormat(fullPath); }
                    catch (Exception ex)
                    { exception = ex; }

                    if (exception != null)
                    {
                        yield return new Issue(GetTemplate("Exception"), null,
                            hitSoundFile, exception.Message);
                        continue;
                    }

                    if (actualFormat == ManagedBass.ChannelType.OGG)
                        yield return new Issue(GetTemplate("ogg"), null,
                            hitSoundFile);

                    // The .mp3 format includes inherent delays and are as such not fit for active hit sounding.
                    else if (actualFormat == ManagedBass.ChannelType.MP3)
                    {
                        bool foundPassiveMp3 = false;
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
                                        hitSoundFile.StartsWith(aSample.GetFileName() + ".")))
                                {
                                    yield return new Issue(GetTemplate("mp3"), null,
                                        hitSoundFile, Timestamp.Get(hitObject), beatmap);

                                    foundPassiveMp3 = true;
                                    break;
                                }
                            }
                            if (foundPassiveMp3)
                                break;
                        }
                    }
                    else
                    {
                        if ((ManagedBass.ChannelType.Wave & actualFormat) == 0)
                            yield return new Issue(GetTemplate("Unexpected Format"), null,
                                hitSoundFile, Audio.EnumToString(actualFormat));

                        else if (!hitSoundFile.EndsWith(".wav"))
                            yield return new Issue(GetTemplate("Incorrect Extension"), null,
                                hitSoundFile, Audio.EnumToString(actualFormat));

                    }
                }
            }
        }
    }
}
