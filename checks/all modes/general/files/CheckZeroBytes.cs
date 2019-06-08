using MapsetParser.objects;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.general.files
{
    public class CheckZeroBytes : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Files",
            Message = "0-byte files.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring all files can be uploaded properly during the submission process."
                },
                {
                    "Reasoning",
                    @"
                    0-byte files prevent other files in the song folder from properly uploading."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "0-byte",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "\"{0}\"",
                        "path")
                    .WithCause(
                        "A file in the song folder contains no data; consists of 0 bytes.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            foreach (string filePath in aBeatmapSet.songFilePaths)
            {
                FileInfo file = new FileInfo(filePath);

                if (file.Length == 0)
                    yield return new Issue(GetTemplate("0-byte"), null,
                        filePath);
            }
        }
    }
}
