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
                    Discourages potentially detrimental file formats for hit sound files.
                    <image-right>
                        https://i.imgur.com/yAF6pEq.png
                        One of the hit sound files using the MP3 extension, which usually means it's also an MP3 format.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    The MP3 format often includes inherent delays. As such, the Wave or OGG format is preferred for 
                    any hit sound file.
                    <note>
                        Passive objects such as slider tails are not clicked and as such do not need accurate 
                        feedback and may use the mp3 format because of this.
                    </note>
                    <note>
                        Note that extension is not the same thing as format. If you take an MP3 file and change its 
                        extension to "".wav"", for example, it will still be an MP3 file. To change the format of a 
                        file you need to re-encode it.
                    </note>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "mp3",
                    new IssueTemplate(Issue.Level.Problem,
                        "\"{0}\" is using the MP3 format and is used for active hit sounding, see {1} in {2} for example.",
                        "path", "timestamp - ", "beatmap")
                    .WithCause(
                        "A hit sound file is using the MP3 format.") },

                { "Unexpected Format",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" is using an unexpected format: \"{1}\".",
                        "path", "actual format")
                    .WithCause(
                        "A hit sound file is using a format which is neither OGG, Wave, or MP3.") },

                { "Incorrect Extension",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" is using the {1} format, but doesn't use the .wav or .ogg extension.",
                        "path", "actual format")
                    .WithCause(
                        "A hit sound file is using an incorrect extension.") },

                { "Exception",
                    new IssueTemplate(Issue.Level.Error,
                        Common.FILE_EXCEPTION_MESSAGE,
                        "path", "exception info")
                    .WithCause(
                        "An error occurred trying to check the format of a hit sound file.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            if (beatmapSet.hitSoundFiles != null)
            {
                foreach (string hitSoundFile in beatmapSet.hitSoundFiles)
                {
                    string fullPath = Path.Combine(beatmapSet.songPath, hitSoundFile);

                    ManagedBass.ChannelType actualFormat = 0;
                    Exception exception = null;
                    try
                        { actualFormat = Audio.GetFormat(fullPath); }
                    catch (Exception ex)
                        { exception = ex; }

                    if (exception != null)
                    {
                        yield return new Issue(GetTemplate("Exception"), null,
                            hitSoundFile, Common.AsExceptionDiv(exception));
                        continue;
                    }

                    // The .mp3 format includes inherent delays and are as such not fit for active hit sounding.
                    if (actualFormat == ManagedBass.ChannelType.MP3)
                    {
                        bool foundActiveMp3 = false;
                        foreach (Beatmap beatmap in beatmapSet.beatmaps)
                        {
                            foreach (HitObject hitObject in beatmap.hitObjects)
                            {
                                if (hitObject is Spinner)
                                    continue;

                                // Only the hit sound edge at which the object is clicked is considered active.
                                if (hitObject.usedHitSamples.Any(sample =>
                                        sample.time == hitObject.time &&
                                        sample.hitSource == HitSample.HitSource.Edge &&
                                        sample.SameFileName(hitSoundFile)))
                                {
                                    yield return new Issue(GetTemplate("mp3"), null,
                                        hitSoundFile, Timestamp.Get(hitObject), beatmap);

                                    foundActiveMp3 = true;
                                    break;
                                }
                            }
                            if (foundActiveMp3)
                                break;
                        }
                    }
                    else
                    {
                        if ((ManagedBass.ChannelType.Wave & actualFormat) == 0 &&
                            (ManagedBass.ChannelType.OGG & actualFormat) == 0)
                        {
                            yield return new Issue(GetTemplate("Unexpected Format"), null,
                                hitSoundFile, Audio.EnumToString(actualFormat));
                        }
                        else if (!hitSoundFile.ToLower().EndsWith(".wav") && !hitSoundFile.ToLower().EndsWith(".ogg"))
                            yield return new Issue(GetTemplate("Incorrect Extension"), null,
                                hitSoundFile, Audio.EnumToString(actualFormat));

                    }
                }
            }
        }
    }
}
