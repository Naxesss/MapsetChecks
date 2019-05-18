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

namespace MapsetChecks.checks.spread
{
    public class CheckLowestDiff : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Spread",
            Message = "Lowest difficulty too difficult for the given drain/play time(s).",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Unrankable",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "With a lowest difficulty {0}, the drain/play time of {1} must be at least {2}, currently {3}.",
                        "lowest diff", "beatmap", "lowest drain", "current drain")
                    .WithCause(
                        "The lowest difficulty of a beatmapset is too high of a difficulty level considering the drain time " +
                        "of the other difficulties, alternatively play time if their drain is 80% or more of it and it isn't " +
                        "the top difficulty.") }
            };
        }
        
        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            double hardThreshold   = (3 * 60 + 30) * 1000;
            double insaneThreshold = (4 * 60 + 15) * 1000;
            double expertThreshold = (5 * 60) * 1000;

            float?  lowestStarRating = aBeatmapSet.beatmaps.Min(aBeatmap => aBeatmap.starRating);
            Beatmap lowestBeatmap    = aBeatmapSet.beatmaps.FirstOrDefault(aBeatmap => aBeatmap.starRating == lowestStarRating);

            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
            {
                double drainTime = beatmap.GetDrainTime();
                double playTime  = beatmap.GetPlayTime();
                
                bool canUsePlayTime =
                    drainTime / playTime >= 0.8 &&
                    beatmap.starRating != aBeatmapSet.beatmaps.Max(aBeatmap => aBeatmap.starRating);

                double effectiveTime = canUsePlayTime ? playTime : drainTime;
                
                if (effectiveTime < hardThreshold)
                    yield return new Issue(GetTemplate("Unrankable"), lowestBeatmap,
                        Beatmap.Difficulty.Hard, beatmap, Timestamp.Get(hardThreshold), Timestamp.Get(effectiveTime))
                        .WithInterpretation("difficulty", (int)Beatmap.Difficulty.Hard);

                if (effectiveTime < insaneThreshold)
                    yield return new Issue(GetTemplate("Unrankable"), lowestBeatmap,
                        Beatmap.Difficulty.Insane, beatmap, Timestamp.Get(insaneThreshold), Timestamp.Get(effectiveTime))
                        .WithInterpretation("difficulty", (int)Beatmap.Difficulty.Insane);

                if (effectiveTime < expertThreshold)
                    yield return new Issue(GetTemplate("Unrankable"), lowestBeatmap,
                        Beatmap.Difficulty.Expert, beatmap, Timestamp.Get(expertThreshold), Timestamp.Get(effectiveTime))
                        .WithInterpretation("difficulty", (int)Beatmap.Difficulty.Expert, (int)Beatmap.Difficulty.Ultra);
            }
        }
    }
}
