using MapsetParser.objects;
using MapsetParser.statics;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MapsetChecks.checks.general.audio
{
    [Check]
    public class CheckCommonFinish : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                // This check would take on another meaning if applied to taiko, since there you basically map with hit sounds.
                Beatmap.Mode.Standard,
                Beatmap.Mode.Catch,
                Beatmap.Mode.Mania
            },
            Category = "Audio",
            Message = "Frequent finish hit sounds.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Discouraging normal/soft finish samples from playing too often to the point where it gets obnoxious 
                    without custom hit sounds."
                },
                {
                    "Reasoning",
                    @"
                    Although possibly fine when using custom samples, this will still get very jarring if the player 
                    turns off custom hit sounds and the finishes are used as frequently as claps/whistles, for example."
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Warning Common",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" may be obnoxious without custom samples. Used most commonly in {1}.",
                        "path", "[difficulty]")
                    .WithCause(
                        "The usage of non-drum finish hit sounds to drain time ratio in a map is 2 seconds or more.") },

                { "Warning Timestamp",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" may be obnoxious without custom samples. Used most frequently leading up to {1}.",
                        "path", "timestamp in [difficulty]")
                    .WithCause(
                        "Non-drum finish hit sounds are used frequently in a short timespan.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (string hsFile in beatmapSet.hitSoundFiles)
            {
                HitSample sample = new HitSample(hsFile);
                if (sample.sampleset == Beatmap.Sampleset.Drum ||
                    sample.hitSound != HitObject.HitSound.Finish ||
                    sample.hitSource != HitSample.HitSource.Edge)
                    continue;

                Common.CollectHitSoundFrequency(beatmapSet, hsFile, scoreThreshold: 6,
                    out string mostFrequentTimestamp, out Dictionary<Beatmap, int> uses);

                if (mostFrequentTimestamp != null)
                    yield return new Issue(GetTemplate("Warning Timestamp"), null,
                        hsFile, mostFrequentTimestamp);
                else
                {
                    Beatmap mapCommonlyUsedIn =
                        Common.GetBeatmapCommonlyUsedIn(beatmapSet, uses, commonUsageThreshold: 2000);

                    if (mapCommonlyUsedIn != null)
                        yield return new Issue(GetTemplate("Warning Common"), null,
                            hsFile, mapCommonlyUsedIn);
                }
            }

        }
    }
}
