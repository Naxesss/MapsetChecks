using System.Collections.Generic;
using System.Linq;
using MapsetParser.objects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;

namespace MapsetChecks.Checks.Taiko.Timing
{
    [Check]
    public class CheckInconsistentBarLines : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Modes = new Beatmap.Mode[]
            {
                Beatmap.Mode.Taiko
            },
            Category = "Timing",
            Message = "Inconsistent omitted bar lines.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Ensuring that bar lines are consistent between all difficulties."
                },
                {
                    "Reasoning",
                    @"
                    Since all difficulties in a set are based around a single song, and bar lines are meant to act as a point of reference
                    for timing in gameplay, it would make the most sense if all difficulties would use the same bar lines. For this reason,
                    if one difficulty skips one, others probably should too."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "Inconsistent",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} Inconsistent omitted bar line, see {1}.",
                        "timestamp - ", "difficulty")
                    .WithCause(
                        "A beatmap does not omit bar line where the reference beatmap does, or visa versa.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            IEnumerable<Beatmap> taikoBeatmaps = beatmapSet.beatmaps.Where(beatmap => beatmap.generalSettings.mode == Beatmap.Mode.Taiko);
            Beatmap refBeatmap = taikoBeatmaps.First();
            foreach (Beatmap beatmap in taikoBeatmaps)
            {
                foreach (UninheritedLine line in refBeatmap.timingLines.OfType<UninheritedLine>())
                {
                    UninheritedLine respectiveLine =
                        beatmap.timingLines.OfType<UninheritedLine>().FirstOrDefault(
                            otherLine => Timestamp.Round(otherLine.offset) == Timestamp.Round(line.offset));

                    if (respectiveLine == null)
                        // Inconsistent lines, which is the responsibility of another check, so we skip this case.
                        continue;

                    double offset = Timestamp.Round(line.offset);

                    if (line.omitsBarLine != respectiveLine.omitsBarLine)
                        yield return new Issue(GetTemplate("Inconsistent"), beatmap,
                            Timestamp.Get(offset), refBeatmap);
                }
            }
        }
    }
}
