using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MapsetChecks.checks.timing
{
    public class CheckInconsistentLines : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Timing",
            Message = "Inconsistent uninherited lines, meter signatures or BPM.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Missing",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} Missing uninherited line, see {1}.",
                        "timestamp - ", "difficulty")
                    .WithCause(
                        "A beatmap does not have an uninherited line which the reference beatmap does, or visa versa.") },

                { "Inconsistent Meter",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} Inconsistent meter signature, see {1}.",
                        "timestamp - ", "difficulty")
                    .WithCause(
                        "The meter signature of an uninherited timing line is different from the reference beatmap.") },

                { "Inconsistent BPM",
                    new IssueTemplate(Issue.Level.Unrankable,
                         "{0} Inconsistent BPM, see {1}.",
                        "timestamp - ", "difficulty")
                    .WithCause(
                        "Same as the meter check, except checks BPM instead.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            Beatmap refBeatmap = aBeatmapSet.beatmaps[0];
            string version = refBeatmap.metadataSettings.version;

            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
            {
                foreach (TimingLine line in refBeatmap.timingLines)
                {
                    if (line.uninherited)
                    {
                        UninheritedLine otherLine =
                            (UninheritedLine)beatmap.timingLines.FirstOrDefault(
                                aLine => aLine.offset == line.offset && aLine.uninherited);

                        double offset = (int)Math.Floor(line.offset);
                        
                        if (otherLine == null)
                            yield return new Issue(GetTemplate("Missing"), beatmap,
                                Timestamp.Get(offset), refBeatmap);
                        else
                        {
                            if (line.meter != otherLine.meter)
                                yield return new Issue(GetTemplate("Inconsistent Meter"), beatmap,
                                    Timestamp.Get(offset), refBeatmap);

                            if (((UninheritedLine)line).msPerBeat != otherLine.msPerBeat)
                                yield return new Issue(GetTemplate("Inconsistent BPM"), beatmap,
                                    Timestamp.Get(offset), refBeatmap);
                        }
                    }
                }
                
                // Check the other way around as well, to make sure the reference map has all uninherited lines this map has.
                foreach (TimingLine line in beatmap.timingLines)
                {
                    if (line.uninherited)
                    {
                        UninheritedLine otherLine =
                            (UninheritedLine)refBeatmap.timingLines.FirstOrDefault(
                                aLine => aLine.offset == line.offset && aLine.uninherited);

                        double offset = (int)Math.Floor(line.offset);
                        
                        if (otherLine == null)
                            yield return new Issue(GetTemplate("Missing"), refBeatmap,
                                Timestamp.Get(offset), beatmap);
                    }
                }
            }
        }
    }
}
