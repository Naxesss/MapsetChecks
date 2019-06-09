using MapsetParser.objects;
using MapsetParser.settings;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MapsetChecks.checks.general.metadata
{
    public class CheckMarkerSpacing : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Metadata",
            Message = "Incorrect marker spacing.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Standardizing the way metadata is written for ranked content.
                    <image>
                        https://i.imgur.com/9w1fzvB.png
                        The romanized artist field is missing a whitespace before and after ""(CV:"". 
                        The unicode artist field is fine, though, since the characters are full-width.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Small deviations in metadata or obvious mistakes in its formatting or capitalization are for the 
                    most part eliminated through standardization. Standardization also reduces confusion in case of 
                    multiple correct ways to write certain fields and contributes to making metadata more consistent 
                    across official content."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Unrankable",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "Missing {0} in {1}; \"{2}\".",
                        "something", "field", "field")
                    .WithCause(
                        "Some whitespace or parameter is missing from the artist or title fields where only alphabetic " +
                        "and numerical characters are involved.") },

                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "Missing {0} in {1}; \"{2}\".",
                        "something", "field", "field")
                    .WithCause(
                        "Same as the other check, but can involve any type of characters.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            Beatmap beatmap = aBeatmapSet.beatmaps[0];
            
            List<Func<string, string>> problemTests = new List<Func<string, string>>()
            {
                aField => FormalSpaceRegex(aField, "CV:"),
                aField => FormalSpaceRegex(aField, "vs\\."),
                aField => FormalSpaceRegex(aField, "feat\\."),
                aField => FormalSpaceRegex(aField, "&"),

                aField => new Regex("[a-zA-Z0-9]\\(CV:").IsMatch(aField) ? "whitespace before \"(CV:\"" : null,
                    // also check before any parenthesis CV: might have before it
                
                aField => new Regex("(?<!\\()CV:").IsMatch(aField) ? "\"(\" before \"CV:\"" : null,
                    // implied from the "Character (CV: Voice Actor)" format requirement
                
                aField => new Regex(",[a-zA-Z0-9]").IsMatch(aField) ? "whitespace after \",\"" : null,
                    // comma only checks trailing whitespaces
            };
            List<Func<string, string>> warningTests = new List<Func<string, string>>()
            {
                // Markers only need spaces around them if both of the following are true
                // - They are not full width (i.e. "、" for comma and "：" for colon)
                // - The character after/before is not full width (i.e. most chinese/japanese characters)
                // Source: Lanturn (metadata helper at the time of writing this)

                // The regex "[一-龠]+|[ぁ-ゔ]+|[ァ-ヴー]" matches all japanese characters.
                
                aField => new Regex("(?<! |\\()feat\\.").IsMatch(aField) ? "whitespace before \"feat.\""    : null,
                aField => new Regex("(?<! )\\(feat\\.") .IsMatch(aField) ? "whitespace before \"(feat.\""   : null,
                aField => new Regex("(?<! )vs\\.")      .IsMatch(aField) ? "whitespace before \"vs.\""      : null,
                aField => new Regex("(?<! )&")          .IsMatch(aField) ? "whitespace before \"&\""        : null,

                aField => new Regex("CV(?!:[ 一-龠]+|[ぁ-ゔ]+|[ァ-ヴー]|：)")  .IsMatch(aField) ? "whitespace after \"CV:\" or full-width colon \"：\"" : null,
                aField => new Regex(",(?![ 一-龠]+|[ぁ-ゔ]+|[ァ-ヴー])")       .IsMatch(aField) ? "whitespace after \",\" or full-width comma \"、\""   : null,

                aField => new Regex("feat\\.(?! )") .IsMatch(aField) ? "whitespace after \"feat.\"" : null,
                aField => new Regex("vs\\.(?! )")   .IsMatch(aField) ? "whitespace after \"vs.\""   : null,
                aField => new Regex("&(?! )")       .IsMatch(aField) ? "whitespace after \"&\""     : null,
            };

            MetadataSettings metadata = beatmap.metadataSettings;
            List<Tuple<string, string>> fields = new List<Tuple<string, string>>()
                { new Tuple<string, string>(metadata.artist,         "artist"),
                  new Tuple<string, string>(metadata.artistUnicode,  "artist unicode"),
                  new Tuple<string, string>(metadata.title,          "title"),
                  new Tuple<string, string>(metadata.titleUnicode,   "title unicode") };

            List<Tuple<Tuple<string, string>, string, bool>> issueMessages = new List<Tuple<Tuple<string, string>, string, bool>>();
            foreach (Tuple<string, string> field in fields)
            {
                foreach (Func<string, string> problemTest in problemTests)
                {
                    string message = problemTest(field.Item1);
                    if (message != null && !issueMessages.Any(aTuple => aTuple.Item2 == message && aTuple.Item1 == field))
                        issueMessages.Add(new Tuple<Tuple<string, string>, string, bool>(field, message, true));
                }

                foreach (Func<string, string> warningTest in warningTests)
                {
                    string message = warningTest(field.Item1);
                    if (message != null && !issueMessages.Any(aTuple => aTuple.Item2 == message && aTuple.Item1 == field))
                        issueMessages.Add(new Tuple<Tuple<string, string>, string, bool>(field, message, false));
                }
            }

            foreach (Tuple<Tuple<string, string>, string, bool> messageTuple in issueMessages)
                yield return new Issue(GetTemplate(messageTuple.Item3 ? "Unrankable" : "Warning"), null,
                    messageTuple.Item2, messageTuple.Item1.Item2,
                    messageTuple.Item1.Item1);
        }

        /// <summary> Returns a message describing where a space is missing given a field and what is tested against. </summary>
        private string FormalSpaceRegex(string aField, string aTest)
        {
            if (new Regex(aTest + "[a-zA-Z0-9]").IsMatch(aField))
                return "whitespace after \"" + aTest.Replace("\\", "") + "\"";

            if (new Regex("[a-zA-Z0-9]" + aTest).IsMatch(aField))
                return "whitespace before \"" + aTest.Replace("\\", "") + "\"";

            return null;
        }

        /// <summary> Applies a predicate to all artist and title metadata fields. Yields an issue wherever the predicate is true. </summary>
        private IEnumerable<Issue> GetFormattingIssues(MetadataSettings aSettings, Func<string, bool> aFunc)
        {
            if (aFunc(aSettings.artist))
                yield return new Issue(GetTemplate("Wrong Format"), null,
                    "Romanized", "artist", aSettings.artist);

            // Unicode fields do not exist in file version 9.
            if (aSettings.artistUnicode != null && aFunc(aSettings.artistUnicode))
                yield return new Issue(GetTemplate("Wrong Format"), null,
                    "Unicode", "artist", aSettings.artistUnicode);

            if (aFunc(aSettings.title))
                yield return new Issue(GetTemplate("Wrong Format"), null,
                    "Romanized", "title", aSettings.title);

            if (aSettings.titleUnicode != null && aFunc(aSettings.titleUnicode))
                yield return new Issue(GetTemplate("Wrong Format"), null,
                    "Unicode", "title", aSettings.titleUnicode);
        }
    }
}
