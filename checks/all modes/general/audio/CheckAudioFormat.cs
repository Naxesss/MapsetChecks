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
                    Ensures that all audio files used for the song itself is in MP3 format."
                },
                {
                    "Reasoning",
                    @"
                    Although the Wave format can support compressed audio, it is usually not efficient and takes up 
                    more space than the MP3 format. OGG can be used for hit sounds, but is deprecated for song audio 
                    files, and is as such not allowed in this case.
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
                        "\"{0}\" is using the {1} format. Song audio files must be in the MP3 format.",
                        "path", "actual format")
                    .WithCause(
                        "A song audio file is not using the MP3 format.") },

                { "Incorrect Extension",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" is using the {1} format, but doesn't use the .mp3 extension.",
                        "path", "actual format")
                    .WithCause(
                        "A song audio file is using an incorrect extension.") },

                { "Exception",
                    new IssueTemplate(Issue.Level.Error,
                        "\"{0}\" returned exception \"{1}\", so unable to check that.",
                        "path", "exception")
                    .WithCause(
                        "An error occurred trying to check the format of a song audio file.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            string audioPath = aBeatmapSet.GetAudioFilePath();
            string audioName = aBeatmapSet.GetAudioFileName();
            if (audioPath != null)
            {
                ManagedBass.ChannelType actualFormat = 0;
                Exception exception = null;
                try
                { actualFormat = Audio.GetFormat(audioPath); }
                catch (Exception ex)
                { exception = ex; }

                if (exception != null)
                    yield return new Issue(GetTemplate("Exception"), null,
                        audioName, exception.Message);

                if ((ManagedBass.ChannelType.MP3 & actualFormat) == 0)
                    yield return new Issue(GetTemplate("Incorrect Format"), null,
                        audioName, Audio.EnumToString(actualFormat));
                else if (!audioName.EndsWith(".mp3"))
                    yield return new Issue(GetTemplate("Incorrect Extension"), null,
                        audioName, Audio.EnumToString(actualFormat));
            }
        }
    }
}
