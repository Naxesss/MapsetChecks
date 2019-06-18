using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
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
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring that the song is timed consistently for all difficulties."
                },
                {
                    "Reasoning",
                    @"
                    Since all difficulties in a set are based around a single song, they should all use the same base timing, 
                    which is made from uninherited lines. Even if a line isn't used by some difficulty due to there being a 
                    break or similar, they still affect things like the main menu flashing and beats/snares/finishes in the 
                    nightcore mod.
                    <note>
                        This matters less for sections without hit objects or with spinners as it's less intrusive to gameplay, 
                        which is the same reason why you don't need to perfectly time intro/breaks in complex timing either.
                    </note>
                    <br \>
                    Similar to metadata, timing (bpm/meter/offset of uninherited lines) should really just be global for the 
                    whole beatmapset rather than difficulty-specific."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Missing Problem",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} Missing uninherited line, see {1}.",
                        "timestamp - ", "difficulty")
                    .WithCause(
                        "A beatmap does not have an uninherited line which the reference beatmap does, or visa versa.") },

                { "Missing Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Missing uninherited line, see {1}. If complex timing this is optional, since there are no hit objects.",
                        "timestamp - ", "difficulty")
                    .WithCause(
                        "Same as the other check, but there are no hit objects from where this line starts to the next uninherited.") },

                { "Inconsistent Meter Problem",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} Inconsistent meter signature, see {1}.",
                        "timestamp - ", "difficulty")
                    .WithCause(
                        "The meter signature of an uninherited timing line is different from the reference beatmap.") },

                { "Inconsistent Meter Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Inconsistent meter signature, see {1}. If complex timing this is optional, since there are no hit objects.",
                        "timestamp - ", "difficulty")
                    .WithCause(
                        "Same as the other check, but there are no hit objects from where this line starts to the next uninherited.") },

                { "Inconsistent BPM Problem",
                    new IssueTemplate(Issue.Level.Problem,
                         "{0} Inconsistent BPM, see {1}.",
                        "timestamp - ", "difficulty")
                    .WithCause(
                        "Same as the meter check, except checks BPM instead.") },

                { "Inconsistent BPM Warning",
                    new IssueTemplate(Issue.Level.Problem,
                         "{0} Inconsistent BPM, see {1}. If complex timing this is optional, since there are no hit objects.",
                        "timestamp - ", "difficulty")
                    .WithCause(
                        "Same as the other check, but there are no hit objects from where this line starts to the next uninherited.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            Beatmap refBeatmap = aBeatmapSet.beatmaps[0];
            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
            {
                foreach (TimingLine line in refBeatmap.timingLines)
                {
                    if (line is UninheritedLine uninheritLine)
                    {
                        UninheritedLine otherUninheritLine =
                            beatmap.timingLines.OfType<UninheritedLine>().FirstOrDefault(
                                aLine => aLine.offset == uninheritLine.offset);
                        
                        HitObject nextHitObject = beatmap.GetNextHitObject(uninheritLine.offset);
                        while(nextHitObject is Spinner || nextHitObject.time == beatmap.GetNextHitObject(nextHitObject.time).time)
                            nextHitObject = beatmap.GetNextHitObject(nextHitObject.time);

                        // TODO: Waiting for pishi's thoughts on having inconsistent uninherited line presence in intro/breaks be a warning instead of problem
                        bool hasHitObjects = beatmap.GetNextTimingLine<UninheritedLine>(nextHitObject.time).offset == uninheritLine.offset;
                        string templateSeverity = hasHitObjects ? " Problem" : " Warning";

                        double offset = Timestamp.Round(uninheritLine.offset);
                        
                        if (otherUninheritLine == null)
                            yield return new Issue(GetTemplate("Missing" + templateSeverity), beatmap,
                                Timestamp.Get(offset), refBeatmap);
                        else
                        {
                            if (uninheritLine.meter != otherUninheritLine.meter)
                                yield return new Issue(GetTemplate("Inconsistent Meter" + templateSeverity), beatmap,
                                    Timestamp.Get(offset), refBeatmap);

                            if (uninheritLine.msPerBeat != otherUninheritLine.msPerBeat)
                                yield return new Issue(GetTemplate("Inconsistent BPM" + templateSeverity), beatmap,
                                    Timestamp.Get(offset), refBeatmap);
                        }
                    }
                }
                
                // Check the other way around as well, to make sure the reference map has all uninherited lines this map has.
                foreach (TimingLine line in beatmap.timingLines)
                {
                    if (line is UninheritedLine)
                    {
                        UninheritedLine otherLine =
                            refBeatmap.timingLines.OfType<UninheritedLine>().FirstOrDefault(
                                aLine => aLine.offset == line.offset);

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
