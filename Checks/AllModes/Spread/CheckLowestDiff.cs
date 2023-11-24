﻿using System.Collections.Generic;
using System.Linq;
using MapsetParser.objects;
using MapsetParser.statics;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;

namespace MapsetChecks.Checks.AllModes.Spread
{
    [Check]
    public class CheckLowestDiff : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Category = "Spread",
            Message = "Lowest difficulty too difficult for the given drain/play time(s).",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Ensuring that newer players still have new content to play at the same time as encouraging mappers to map longer songs.
                    <image>
                        https://i.imgur.com/1PFVl76.png
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
            return new Dictionary<string, IssueTemplate>
            {
                { "Problem",
                    new IssueTemplate(Issue.Level.Problem,
                        "With a lowest difficulty {0}, the {1} time of {2} must be at least {3}, currently {4}.",
                        "lowest diff", "drain/play", "beatmap", "lowest drain", "current drain")
                    .WithCause(
                        "The lowest difficulty of a beatmapset is too high of a difficulty level considering the drain time " +
                        "of the other difficulties, alternatively play time if their drain is 80% or more of it and it isn't " +
                        "the top difficulty.") }
            };
        }
        
        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            const double hardThreshold = (3 * 60 + 30) * 1000;
            const double insaneThreshold = (4 * 60 + 15) * 1000;
            const double expertThreshold = 5 * 60 * 1000;
            
            const double breakTimeLeniency = 30 * 1000;
            
            var lowestBeatmap = beatmapSet.beatmaps.First();

            foreach (var beatmap in beatmapSet.beatmaps)
            {
                double drainTime = beatmap.GetDrainTime();
                double playTime  = beatmap.GetPlayTime();
                double breakTime = playTime - drainTime;
                
                bool isHighestDifficulty = beatmapSet.beatmaps.Last().metadataSettings.version ==
                                           beatmap.metadataSettings.version;
                bool canUsePlayTime = !isHighestDifficulty || breakTime < breakTimeLeniency;

                double effectiveTime = playTime;
                double thresholdReduction = 0;
                
                if (!canUsePlayTime)
                {
                    effectiveTime = drainTime;
                    
                    // We can't use play time, so we end up with drain time + break time leniency instead.
                    // Hence, drain time thresholds are effectively the break time leniency lower.
                    thresholdReduction = breakTimeLeniency;
                }

                if (effectiveTime < hardThreshold - thresholdReduction)
                    yield return new Issue(GetTemplate("Problem"), lowestBeatmap,
                        Beatmap.Difficulty.Hard, canUsePlayTime ? "play" : "drain", beatmap,
                        Timestamp.Get(hardThreshold - thresholdReduction), Timestamp.Get(effectiveTime))
                        .ForDifficulties(Beatmap.Difficulty.Hard);

                if (effectiveTime < insaneThreshold - thresholdReduction)
                    yield return new Issue(GetTemplate("Problem"), lowestBeatmap,
                        Beatmap.Difficulty.Insane, canUsePlayTime ? "play" : "drain", beatmap,
                        Timestamp.Get(insaneThreshold - thresholdReduction), Timestamp.Get(effectiveTime))
                        .ForDifficulties(Beatmap.Difficulty.Insane);

                if (effectiveTime < expertThreshold - thresholdReduction)
                    yield return new Issue(GetTemplate("Problem"), lowestBeatmap,
                        Beatmap.Difficulty.Expert, canUsePlayTime ? "play" : "drain", beatmap,
                        Timestamp.Get(expertThreshold - thresholdReduction), Timestamp.Get(effectiveTime))
                        .ForDifficulties(Beatmap.Difficulty.Expert, Beatmap.Difficulty.Ultra);
            }
        }
    }
}
