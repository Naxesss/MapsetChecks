using MapsetParser.objects;
using MapsetParser.statics;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using MapsetVerifierFramework.objects.resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.general.audio
{
    [Check]
    public class CheckHitSoundImbalance : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Audio",
            Message = "Imbalanced hit sounds.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing the audio channels of hit sounds from being noticeably imbalanced, for example the left 
                    channel being twice as loud as the right.
                    <image>
                        https://i.imgur.com/6if5mJO.png
                        A hit sound which is much louder in the left channel compared to the right, as shown in Audacity.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Having noticeably imbalanced hit sounds is often jarring, especially if used frequently or consecutively."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Warning Silent",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" is completely silent in the {1} channel.",
                        "path", "left/right")
                    .WithCause(
                        "One of the channels of a hit sound has no volume, but still 2 channels.") },

                { "Warning Common",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" has a notably louder {1} channel. Can be found commonly in for example {2}.",
                        "path", "left/right", "[difficulty]")
                    .WithCause(
                        "One of the channels of a hit sound has at least half the total volume of the other. " +
                        "The hit sound must also be used on average once every 10 seconds in a map.") },

                { "Warning Timestamp",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" has a notably louder {1} channel. Used pretty frequently leading up to {2}.",
                        "path", "left/right", "timestamp in [difficulty]")
                    .WithCause(
                        "Same as the other check, except only happens when the hit sound is used frequently in a short timespan.") },

                { "Minor",
                    new IssueTemplate(Issue.Level.Minor,
                        "\"{0}\" has a notably louder {1} channel, not a huge deal in this case though.",
                        "path", "left/right")
                    .WithCause(
                        "One of the channels of a hit sound has half the total volume of the other.") },

                { "Unable to check",
                    new IssueTemplate(Issue.Level.Error,
                        "\"{0}\" {1}, so unable to check that.",
                        "path", "error")
                    .WithCause(
                        "There was an error parsing a hit sound file.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            foreach (string hsFile in aBeatmapSet.hitSoundFiles)
            {
                string hsPath = Path.Combine(aBeatmapSet.songPath, hsFile);

                int channels = 0;
                List<float[]> peaks = null;
                Exception exception = null;
                try
                {
                    channels = Audio.GetChannels(hsPath);
                    peaks    = Audio.GetPeaks(hsPath);
                }
                catch (Exception ex)
                { exception = ex; }

                // Cannot yield in catch statements, hence externally handled.
                if (exception != null)
                {
                    yield return new Issue(GetTemplate("Unable to check"), null,
                        hsFile, String.Join(" ", exception));
                    continue;
                }

                // Mono can't be imbalanced since it's the same audio on both sides.
                if (channels < 2)
                    continue;

                // No peaks means 44 byte silent.
                if (peaks.Count == 0)
                    continue;

                float leftSum = 0;
                float rightSum = 0;

                leftSum = peaks.Sum(aPeak => aPeak?[0] ?? 0);
                rightSum = peaks.Sum(aPeak => aPeak.Count() > 1 ? aPeak?[1] ?? 0 : 0);

                if (leftSum == 0 || rightSum == 0)
                {
                    yield return new Issue(GetTemplate("Warning Silent"), null,
                        hsFile, leftSum - rightSum > 0 ? "left" : "right");
                    continue;
                }

                // 2 would mean one is double the sum of the other.
                float relativeVolume =
                    leftSum > rightSum ?
                        leftSum / rightSum :
                        rightSum / leftSum;

                if (relativeVolume < 2)
                    continue;

                // Imbalance is only an issue if it is used frequently in a short timespan or it's overall common.
                // So here we do a scoring mechanism to do the former part.
                Dictionary<Beatmap, int> uses = new Dictionary<Beatmap, int>();
                double prevTime = 0;
                double frequencyScore = 0;
                string timestamp = "";
                foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
                {
                    uses[beatmap] = 0;
                    prevTime = beatmap.hitObjects.FirstOrDefault()?.time ?? 0;
                    foreach (HitObject hitObject in beatmap.hitObjects)
                    {
                        if (!hitObject.GetUsedHitSamples().Any(aSample => aSample.SameFileName(hsFile)))
                            continue;

                        frequencyScore *= Math.Pow(0.8, 1 / 1000 * prevTime);
                        prevTime = hitObject.time;

                        ++uses[beatmap];
                        frequencyScore += beatmap.generalSettings.mode == Beatmap.Mode.Mania ? 0.5 : 1;

                        if (frequencyScore < 10 / relativeVolume)
                            continue;

                        timestamp = $"{Timestamp.Get(hitObject)} in {beatmap}";
                        break;
                    }

                    if (timestamp.Length > 0)
                        break;
                }

                if (timestamp.Length > 0)
                    yield return new Issue(GetTemplate("Warning Timestamp"), null,
                        hsFile, leftSum - rightSum > 0 ? "left" : "right", timestamp);
                else
                {
                    // For the latter part we arbitrarily choose 10 seconds on average as common.
                    // Has to be done on each map individually as hit sounding can vary between them.
                    Beatmap commonMap =
                        aBeatmapSet.beatmaps.FirstOrDefault(aBeatmap =>
                        {
                            if (uses[aBeatmap] == 0)
                                return false;

                            // Mania can have multiple objects per moment in time, so we arbitrarily divide its usage by 2.
                            return
                                aBeatmap.GetDrainTime() /
                                (aBeatmap.generalSettings.mode == Beatmap.Mode.Mania ?
                                        uses[aBeatmap] / 2 :
                                        uses[aBeatmap])
                                    > 10000;
                        });

                    if (commonMap != null)
                        yield return new Issue(GetTemplate("Warning Common"), null,
                            hsFile, leftSum - rightSum > 0 ? "left" : "right", commonMap);
                    else
                        yield return new Issue(GetTemplate("Minor"), null,
                            hsFile, leftSum - rightSum > 0 ? "left" : "right");
                }
            }
        }
    }
}
