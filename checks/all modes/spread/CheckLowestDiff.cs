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
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring that newer players still have new content to play at the same time as encouraging mappers to map longer songs.
                    <image>
                        assets/docs/lowestDiff.jpg
                        The drain time thresholds determining the highest difficulty level for the lowest difficulty in the set.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Newer players usually struggle with especially long songs, so encouraging them to try shorter songs first at lower 
                    difficulty levels allows them to learn the basics before trying to train their stamina or similar. This is done by 
                    requiring that shorter songs have lower difficulties, while longer songs can have less of them. This also reduces the 
                    workload on mappers and as such introduces a larger variety of songs into the game that otherwise wouldn't be so common 
                    due to their length.
                    <note>
                        The star rating algorithm is currently only implemented for standard, so the suggested difficulty level of beatmaps 
                        not from standard is highly inaccurate. Changing the interpretation of difficulty levels manually will fix this.
                    </note>"
                }
            }
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
