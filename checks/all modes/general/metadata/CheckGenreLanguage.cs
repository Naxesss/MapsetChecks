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
                    Ensuring that at least one web genre and language field is specified in the tags."
                },
                {
                    "Reasoning",
                    @"
                    Although they're set on the web's genre and language fields, you can't search for 
                    these fields in-game, so that's why they're also put in the tags for consistency."
                }
            }
        };

        private static readonly string[][] genreTagCombinations = {
            new string[] { "Video", "Game" },
            new string[] { "Anime" },
            new string[] { "Rock" },
            new string[] { "Pop" },
            new string[] { "Other" },
            new string[] { "Novelty" },
            new string[] { "Hip", "Hop" },
            new string[] { "Electronic" }
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
            new string[] { "Instrumental" }
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
                    new IssueTemplate(Issue.Level.Problem,
                        "No genre tag was found (\"rock\", \"pop\", \"electronic\", etc).")
                    .WithCause(
                        "None of the following tags were found (case insensitive):" +
                        ToCause(genreTagCombinations)) },

                { "Language",
                    new IssueTemplate(Issue.Level.Problem,
                        "No language tag was found (\"english\", \"japanese\", \"instrumental\", etc).")
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
                tagCombination.All(tag =>
                    tags.Contains(tag.ToLower())
                )
            );
    }
}
