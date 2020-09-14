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
    public class CheckAudioFormat : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Audio",
            Message = "Incorrect audio format.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensures that all audio files used for the song itself is in MP3 or OGG format."
                },
                {
                    "Reasoning",
                    @"
                    Although the Wave format can support compressed audio, it is usually not efficient and takes up 
                    more space than the MP3 format.
                    <note>
                        Note that extension is not the same thing as format. If you take an OGG file and change its 
                        extension to "".mp3"", for example, it will still be a OGG file. To change the format of a 
                        file you need to re-encode it.
                    </note>
                    <br \>
                    MP3 often has an inherent delay, which is why it isn't allowed in active hit sounding. In audio 
                    files, however, this delay can be counteracted with offset."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Incorrect Format",
                    new IssueTemplate(Issue.Level.Problem,
                        "\"{0}\" is using the {1} format. Song audio files must be in either MP3 or OGG format.",
                        "path", "actual format")
                    .WithCause(
                        "A song audio file is not using the MP3 format.") },

                { "Incorrect Extension",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" is using the {1} format, but doesn't use the {2} extension.",
                        "path", "actual format", "expected extension")
                    .WithCause(
                        "A song audio file is using an incorrect extension.") },

                { "Exception",
                    new IssueTemplate(Issue.Level.Error,
                        Common.FILE_EXCEPTION_MESSAGE,
                        "path", "exception info")
                    .WithCause(
                        "An error occurred trying to check the format of a song audio file.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            string audioPath = beatmapSet.GetAudioFilePath();
            string audioName = beatmapSet.GetAudioFileName();
            if (audioPath == null)
                yield break;

            ManagedBass.ChannelType actualFormat = 0;
            Exception exception = null;
            try
                { actualFormat = Audio.GetFormat(audioPath); }
            catch (Exception ex)
                { exception = ex; }

            if (exception != null)
                yield return new Issue(GetTemplate("Exception"), null,
                    audioName, Common.AsExceptionDiv(exception));

            else if ((ManagedBass.ChannelType.MP3 & actualFormat) != ManagedBass.ChannelType.MP3 &&
                     (ManagedBass.ChannelType.OGG & actualFormat) != ManagedBass.ChannelType.OGG)
                yield return new Issue(GetTemplate("Incorrect Format"), null,
                    audioName, Audio.EnumToString(actualFormat));

            else if (!audioName.ToLower().EndsWith(".mp3") && (ManagedBass.ChannelType.MP3 & actualFormat) == ManagedBass.ChannelType.MP3)
                yield return new Issue(GetTemplate("Incorrect Extension"), null,
                    audioName, Audio.EnumToString(actualFormat), ".mp3");

            else if (!audioName.ToLower().EndsWith(".ogg") && (ManagedBass.ChannelType.OGG & actualFormat) == ManagedBass.ChannelType.OGG)
                yield return new Issue(GetTemplate("Incorrect Extension"), null,
                    audioName, Audio.EnumToString(actualFormat), ".ogg");
        }
    }
}
