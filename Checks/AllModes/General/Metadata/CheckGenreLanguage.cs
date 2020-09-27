using MapsetParser.objects;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MapsetChecks.checks.general.metadata
{
    [Check]
    public class CheckGenreLanguage : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Metadata",
            Message = "Missing genre/language in tags.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Consistent searching between web and in-game."
                },
                {
                    "Reasoning",
                    @"
                    Web's language/genre fields can be searched for on the web, but not in-game. 
                    They are therefore added to the tags for consistency.
                    <image-right>
                        https://i.imgur.com/g6zlqhy.png
                        An example of web genre/language also in the tags.
                    </image>"
                }
            }
        };

        private static readonly string[][] genreTagCombinations = {
            new string[] { "Video", "Game" },
            new string[] { "Anime" },
            new string[] { "Rock" },
            new string[] { "Pop" },
            new string[] { "Novelty" },
            new string[] { "Hip", "Hop" },
            new string[] { "Electronic" },
            new string[] { "Metal" },
            new string[] { "Classical" },
            new string[] { "Folk" },
            new string[] { "Jazz" }
        };

        private static readonly string[][] languageTagCombinations = {
            new string[] { "English" },
            new string[] { "Chinese" },
            new string[] { "French" },
            new string[] { "German" },
            new string[] { "Italian" },
            new string[] { "Japanese" },
            new string[] { "Korean" },
            new string[] { "Spanish" },
            new string[] { "Swedish" },
            new string[] { "Russian" },
            new string[] { "Polish" },
            new string[] { "Instrumental" },

            // Following are not web languages, but if we find these in the tags,
            // web would need to be "Other" anyway, so no point in warning.
            new string[] { "Conlang" },
            new string[] { "Hindi" },
            new string[] { "Arabic" },
            new string[] { "Portugese" },
            new string[] { "Turkish" },
            new string[] { "Vietnamese" },
            new string[] { "Persian" },
            new string[] { "Indonesian" },
            new string[] { "Ukrainian" },
            new string[] { "Romanian" },
            new string[] { "Dutch" },
            new string[] { "Thai" },
            new string[] { "Greek" },
            new string[] { "Somali" },
            new string[] { "Malay" },
            new string[] { "Hungarian" },
            new string[] { "Czech" },
            new string[] { "Norwegian" },
            new string[] { "Finnish" },
            new string[] { "Danish" },
            new string[] { "Latvia" },
            new string[] { "Lithuanian" },
            new string[] { "Estonian" },
            new string[] { "Punjabi" },
            new string[] { "Bengali" }
        };

        private string ToCause(string[][] tagCombinations)
        {
            StringBuilder liStr = new StringBuilder();
            foreach (string[] combination in tagCombinations)
                liStr.Append("<li>" + string.Join(" & ", combination.Select(_ => "\"" + _ + "\"")) + "</li>");

            return $"<ul>{liStr}</ul>";
        }

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Genre",
                    new IssueTemplate(Issue.Level.Warning,
                        "Missing genre tag (\"rock\", \"pop\", \"electronic\", etc), ignore if none fit.")
                    .WithCause(
                        "None of the following tags were found (case insensitive):" +
                        ToCause(genreTagCombinations)) },

                { "Language",
                    new IssueTemplate(Issue.Level.Warning,
                        "Missing language tag (\"english\", \"japanese\", \"instrumental\", etc), ignore if none fit.")
                    .WithCause(
                        "None of the following tags were found (case insensitive):" +
                        ToCause(languageTagCombinations)) }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            Beatmap refBeatmap = beatmapSet.beatmaps.FirstOrDefault();
            if (refBeatmap == null)
                yield break;

            string[] tags = refBeatmap.metadataSettings.tags.ToLower().Split(" ");

            if (!HasAnyCombination(genreTagCombinations, tags))
                yield return new Issue(GetTemplate("Genre"), null);

            if (!HasAnyCombination(languageTagCombinations, tags))
                yield return new Issue(GetTemplate("Language"), null);
        }

        /// <summary> Returns true if all tags in any combination exist in the given tags
        /// (e.g. contains both "Video" and "Game", or "Electronic"), case insensitive. </summary>
        private bool HasAnyCombination(string[][] tagCombinations, string[] tags) =>
            tagCombinations.Any(tagCombination =>
                tagCombination.All(tagInCombination =>
                    tags.Any(tag =>
                        tag.Contains(tagInCombination.ToLower())
                    )
                )
            );
    }
}
