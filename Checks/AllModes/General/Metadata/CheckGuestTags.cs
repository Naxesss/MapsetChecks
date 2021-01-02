using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MapsetParser.objects;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;

namespace MapsetChecks.Checks.AllModes.General.Metadata
{
    [Check]
    public class CheckGuestTags : GeneralCheck
    {
        // Matches on a mappers name which can contain any character, number, comma or ampersand.
        private readonly Regex mapperRegex = new Regex(@"^([a-z0-9&, ]+)(s)'|'s", RegexOptions.IgnoreCase);

        // Matches on all used split characters for a collab with the used whitespaces
        private readonly Regex collabSplitCharRegex = new Regex(@" & |, ");

        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Metadata",
            Message = "Missing GDers in tags.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring that the set can be found by searching for any of its guest difficulty creators."
                },
                {
                    "Reasoning",
                    @"
                    If you're looking for beatmaps of a specific user, it'd make sense if sets containing their
                    guest difficulties would appear, and not only sets they hosted."
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" is possessive but \"{1}\" isn't in the tags, ignore if not a user.",
                        "guest's diff", "guest")
                    .WithCause(
                        "A difficulty name is prefixed by text containing an apostrophe (') before or after " +
                        "the character \"s\", which is not in the tags.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (Beatmap beatmap in beatmapSet.beatmaps)
            {
                Match match = mapperRegex.Match(beatmap.metadataSettings.version);

                // e.g. "Alphabet" in "Alphabet's Normal"
                string possessor = match.Groups[1].Value;

                if (match.Groups.Count == 2)
                    // If e.g. "Naxess' Insane", group 1 is "Naxes" and group 2 is the remaining "s".
                    possessor += match.Groups[2].Value;

                // Check if this difficulty contains a collab split character
                if (collabSplitCharRegex.IsMatch(possessor))
                {
                    string[] collabPossessors = collabSplitCharRegex.Split(possessor);

                    foreach (string collabPossessor in collabPossessors)
                    {
                        // Checking the tags is not needed when one of the collab participants is the mapset creator
                        if (beatmap.metadataSettings.creator == collabPossessor 
                            || beatmap.metadataSettings.IsCoveredByTags(collabPossessor))
                            continue;

                        yield return new Issue(GetTemplate("Warning"), null,
                            beatmap.metadataSettings.version, collabPossessor.ToLower().Replace(" ", "_"));
                    }
                }
                else
                {
                    if (beatmap.metadataSettings.IsCoveredByTags(possessor))
                        continue;

                    yield return new Issue(GetTemplate("Warning"), null,
                        beatmap.metadataSettings.version, possessor.ToLower().Replace(" ", "_"));
                }
            }
        }
    }
}
