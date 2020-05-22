using MapsetParser.objects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MapsetChecks.checks.timing
{
    [Check]
    public class CheckWrongSnapping : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Timing",
            Message = "Wrongly or inconsistently snapped hit objects.",
            Author = "Naxess",
            
            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing incorrectly snapped hit objects, for example 1/6 being used where the song only supports 
                    1/4, or a slider tail accidentally being extended 1/16 too far."
                },
                {
                    "Reasoning",
                    @"
                    Should hit objects not align with any audio cue or otherwise recognizable pattern, it would not only 
                    force the player to guess when objects should be clicked, but also harm the perceived connection between 
                    the beatmap and the song, neither of which make for good experiences.
                    <note>
                        Note that this check is intentionally heavy on false-positives for safety's sake due to this being a 
                        common disqualification reason.
                    </note>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                // warnings
                { "Snap Consistency",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} (1/{1}) Different snapping, {2} (1/{3}), is used in {4}.",
                        "timestamp - ", "X", "timestamp - ", "X", "difficulty")
                    .WithCause(
                        "Two hit objects in separate difficulties do not have any object in the other difficulty at the same time, " +
                        "and are close enough in time to be mistaken for one another." +
                        "<note>Ignores cases where the divisor on the lower difficulty is less than on the higher difficulty, since " +
                        "this is usually natural.</note>") },

                { "Snap Count",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} 1/{1} is used 3 times or less, ensure this makes sense.",
                        "timestamp(s) -", "X")
                    .WithCause(
                        "The beat snap divisor a hit object is on is used less than or equal to 3 times in the same difficulty " +
                        "and is 1/6 or lower.") },

                { "Snap Percent",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} 1/{1} makes out 0.5% or less of snappings, ensure this makes sense.",
                        "timestamp(s) -", "X")
                    .WithCause(
                        "The beat snap divisor a hit object is on is used less than or equal to 0.5% of all snappings in the same " +
                        "difficulty and is 1/6 or lower.") },

                // minors
                { "Minor Snap Count",
                    new IssueTemplate(Issue.Level.Minor,
                        "{0} 1/{1} is used 7 times or less, ensure this makes sense.",
                        "timestamp(s) -", "X")
                    .WithCause(
                        "Same as the other check, except with 7 as threshold instead.") },

                { "Minor Snap Percent",
                    new IssueTemplate(Issue.Level.Minor,
                        "{0} 1/{1} makes out 5% or less of snappings, ensure this makes sense.",
                        "timestamp(s) -", "X")
                    .WithCause(
                        "Same as the other check, except with 5% as threshold instead.") }
            };
        }

        private List<int> countWarningDivisors;
        private List<int> countMinorDivisors;
        private List<int> percentWarningDivisors;
        private List<int> percentMinorDivisors;

        private List<Tuple<int, string>> countWarningStamps;
        private List<Tuple<int, string>> countMinorStamps;
        private List<Tuple<int, string>> percentWarningStamps;
        private List<Tuple<int, string>> percentMinorStamps;

        private ConcurrentBag<Tuple<double, double, Beatmap>> inconsistentPlaces;

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            // uses a non-isolated approach in order to compare for inconsistent snappings between beatmaps in a set
            foreach (Beatmap beatmap in beatmapSet.beatmaps)
            {
                inconsistentPlaces = new ConcurrentBag<Tuple<double, double, Beatmap>>();

                IEnumerable<Beatmap> otherBeatmaps =
                    beatmapSet.beatmaps.Where(otherBeatmap =>
                        otherBeatmap.starRating > beatmap.starRating &&
                        otherBeatmap.generalSettings.mode == beatmap.generalSettings.mode);

                Parallel.ForEach(otherBeatmaps, otherBeatmap => PopulateInconsistentPlaces(beatmap, otherBeatmap));

                foreach (double inconsistentTime in inconsistentPlaces.Select(tuple => tuple.Item1).Distinct())
                {
                    Beatmap otherBeatmap =
                        inconsistentPlaces
                            .Where(tuple => tuple.Item1 == inconsistentTime)
                            .Select(tuple => tuple.Item3).First();

                    double stampTime =
                        inconsistentPlaces
                            .Where(tuple => tuple.Item1 == inconsistentTime)
                            .Select(tuple => tuple.Item2).FirstOrDefault();

                    if (beatmap.GetLowestDivisor(stampTime) == 0 || beatmap.GetLowestDivisor(inconsistentTime) == 0)
                        continue;

                    yield return new Issue(GetTemplate("Snap Consistency"), beatmap,
                        Timestamp.Get(stampTime), beatmap.GetLowestDivisor(stampTime),
                        Timestamp.Get(inconsistentTime), beatmap.GetLowestDivisor(inconsistentTime),
                        otherBeatmap);
                }

                PrepareRareDivisors(beatmap);
                PopulateRareDivisors(beatmap);

                foreach (Issue issue in GetDivisorIssues(beatmap, countWarningStamps, "Snap Count")) yield return issue;
                foreach (Issue issue in GetDivisorIssues(beatmap, percentWarningStamps, "Snap Percent")) yield return issue;
                foreach (Issue issue in GetDivisorIssues(beatmap, countMinorStamps, "Minor Snap Count")) yield return issue;
                foreach (Issue issue in GetDivisorIssues(beatmap, percentMinorStamps, "Minor Snap Percent")) yield return issue;
            }
        }

        /// <summary> Finds which divisors are rare in the beatmap so that they can be looked for in other functions. </summary>
        private void PrepareRareDivisors(Beatmap beatmap)
        {
            // rather than calculating every single snapping, this instead just looks for potentially weird snappings
            // this makes it both more convenient, due to constraining unimportant options, and more optimized
            const int countWarning = 3;
            const int countMinor = 7;
            const double percentWarning = 0.005;
            const double percentMinor = 0.05;

            countWarningDivisors = new List<int>();
            countMinorDivisors = new List<int>();
            percentWarningDivisors = new List<int>();
            percentMinorDivisors = new List<int>();

            var edgeDivisors =
                beatmap.hitObjects.SelectMany(hitObject =>
                    hitObject.GetEdgeTimes().Select(time =>
                        beatmap.GetLowestDivisor(time)
                    )
                )
                .Where(divisor => divisor != 0).GroupBy(divisor => divisor).Select(group =>
                    new
                    {
                        divisor = group.Key,
                        count = group.Count()
                    }
                );

            int divisorsTotal = edgeDivisors.Sum(divisorGroup => divisorGroup.count);

            foreach (var divisorGroup in edgeDivisors)
            {
                double precentage = divisorGroup.count / (double)divisorsTotal;

                if (divisorGroup.divisor < 6)
                    continue;

                if (divisorGroup.count <= countWarning) countWarningDivisors.Add(divisorGroup.divisor);
                else if (divisorGroup.count <= countMinor) countMinorDivisors.Add(divisorGroup.divisor);

                if (precentage < percentWarning) percentWarningDivisors.Add(divisorGroup.divisor);
                else if (precentage < percentMinor) percentMinorDivisors.Add(divisorGroup.divisor);
            }
        }

        /// <summary> Populates the warnings and minor issues for rare divisors in the given beatmap. </summary>
        private void PopulateRareDivisors(Beatmap beatmap)
        {
            countWarningStamps = new List<Tuple<int, string>>();
            countMinorStamps = new List<Tuple<int, string>>();
            percentWarningStamps = new List<Tuple<int, string>>();
            percentMinorStamps = new List<Tuple<int, string>>();

            foreach (HitObject hitObject in beatmap.hitObjects)
                foreach (double edgeTime in hitObject.GetEdgeTimes())
                    TryAddDivisorIssue(edgeTime, beatmap);

            countWarningStamps = countWarningStamps.Distinct().ToList();
            countMinorStamps = countMinorStamps.Distinct().ToList();
            percentWarningStamps = percentWarningStamps.Distinct().ToList();
            percentMinorStamps = percentMinorStamps.Distinct().ToList();
        }

        /// <summary> Supplied with the divisor issue list, this function basically just turns them into readable issues
        /// which we can then return in GetIssues. </summary>
        private IEnumerable<Issue> GetDivisorIssues(Beatmap beatmap, List<Tuple<int, string>> divisorTupleList, string templateKey)
        {
            if (divisorTupleList.Count == 0)
                yield break;

            foreach (int divisor in divisorTupleList.Select(stamp => stamp.Item1).Distinct())
            {
                IEnumerable<string> stamps = divisorTupleList.Where(stamp => stamp.Item1 == divisor).Select(stamp => stamp.Item2);

                yield return new Issue(GetTemplate(templateKey), beatmap,
                    String.Join(" ", stamps), divisor);
            }
        }

        /// <summary> Adds the given time value as a divisor issue if its divisor is really rare in the beatmap, and it isn't unsnapped. </summary>
        private void TryAddDivisorIssue(double time, Beatmap beatmap)
        {
            // no need to double error, unsnap check will take care of this
            if (beatmap.GetUnsnapIssue(time) != null)
                return;

            int divisor = beatmap.GetLowestDivisor(time);

            if (!countWarningDivisors  .Contains(divisor) &&
                !percentWarningDivisors.Contains(divisor) &&
                !countMinorDivisors    .Contains(divisor) &&
                !percentMinorDivisors  .Contains(divisor))
                return;

            if      (countWarningDivisors.Contains(divisor))    countWarningStamps  .Add(new Tuple<int, string>(divisor, Timestamp.Get(time)));
            else if (percentWarningDivisors.Contains(divisor))  percentWarningStamps.Add(new Tuple<int, string>(divisor, Timestamp.Get(time)));
            else if (countMinorDivisors.Contains(divisor))      countMinorStamps    .Add(new Tuple<int, string>(divisor, Timestamp.Get(time)));
            else if (percentMinorDivisors.Contains(divisor))    percentMinorStamps  .Add(new Tuple<int, string>(divisor, Timestamp.Get(time)));
        }

        /// <summary> Populates the inconsistent places list, which keeps track of any
        /// time values in either beatmap that has no corresponding value in the other. </summary>
        private void PopulateInconsistentPlaces(Beatmap beatmap, Beatmap otherBeatmap)
        {
            List<double> differenceTimes = GetDifferenceTimes(beatmap, otherBeatmap).ToList();
            List<double> otherDifferenceTimes = GetDifferenceTimes(otherBeatmap, beatmap).ToList();

            foreach (double time in differenceTimes)
                TryAddInconsistentPlace(otherDifferenceTimes, otherBeatmap, time);
        }

        /// <summary> Returns any time value from the first beatmap that has no corresponding
        /// object within 3 ms in the other beatmap. </summary>
        private IEnumerable<double> GetDifferenceTimes(Beatmap beatmap, Beatmap otherBeatmap)
        {
            // TODO: Surely, this can be optimized. May want to swap the `otherBeatmap.hitObjects` loop with
            //       the `hitObject.GetEdgeTimes()` one, for example, in order to improve the time complexity.
            foreach (HitObject hitObject in beatmap.hitObjects)
            {
                foreach (double edgeTime in hitObject.GetEdgeTimes())
                {
                    bool isOverlapping = false;

                    foreach (HitObject otherHitObject in otherBeatmap.hitObjects)
                        foreach (double otherEdgeTime in otherHitObject.GetEdgeTimes())
                            if (Math.Abs(edgeTime - otherEdgeTime) < 3)
                                isOverlapping = true;

                    if (!isOverlapping)
                        yield return edgeTime;
                }
            }
        }

        /// <summary> Adds any point in time where no object in the other beatmap is within 3 ms, but
        /// is within the consistency range, depending on which divisor this point in time is in.<para/>
        /// This usually means the mapper has interpreted the same sound(s) differently from the other beatmap,
        /// so we add it as a potential inconsistency.</summary>
        private void TryAddInconsistentPlace(List<double> differenceTimes, Beatmap otherBeatmap, double otherTime)
        {
            List<double> inconsistencies = differenceTimes.Where(time =>
            {
                if (Math.Abs(time - otherTime) < 3)
                    return false;

                UninheritedLine line = otherBeatmap.GetTimingLine<UninheritedLine>(time);
                double msPerBeat = line.msPerBeat;

                if (Math.Abs(time - otherTime) >= msPerBeat)
                    return false;

                double consistencyRange = GetConsistencyRange(otherBeatmap, time, msPerBeat, otherTime);
                return
                    time + consistencyRange > otherTime &&
                    time - consistencyRange < otherTime;
            }).ToList();

            foreach (double inconsistency in inconsistencies)
                if (!inconsistentPlaces.Any(place => place.Item1 == inconsistency))
                    inconsistentPlaces.Add(new Tuple<double, double, Beatmap>(inconsistency, otherTime, otherBeatmap));
        }

        private readonly int[] divisors = new int[] { 1, 2, 3, 4, 6, 8, 12, 16 };

        /// <summary> Gets a time offset from a given divisor which may be confused with it. Larger for smaller divisors.
        /// So the offset for a 1/1 would be larger than for a 1/6, for example. If two times are supplied, the largest divisor is used. </summary>
        private double GetConsistencyRange(Beatmap otherBeatmap, double time, double msPerBeat, double? otherTime = null)
        {
            int divisor = Math.Max(otherBeatmap.GetLowestDivisor(time), 2);

            if (otherTime == null)
            {
                int index =
                    divisor == 0 ? -1 :
                    Array.IndexOf(divisors, divisor) == -1 ? divisors.Length :
                    Array.IndexOf(divisors, divisor);

                // if the divisor is 0, that means the time we gave it was unsnapped
                if (index == -1)
                    return 2;

                return msPerBeat / divisors.ElementAt(index + 2 > divisors.Length - 1 ? divisors.Length - 1 : index + 2) - 2;
            }

            int higherDiffDivisor = Math.Max(otherBeatmap.GetLowestDivisor(otherTime.GetValueOrDefault()), 2);

            // If the higher difficulty uses higher snaps, that's assumed to be normal progression,
            // unless we go from 1/3 to 1/4 or similar, which would be pretty odd.
            if (divisor < higherDiffDivisor || divisor % 3 != 0 && higherDiffDivisor % 3 == 0)
                return Math.Max(GetConsistencyRange(otherBeatmap, time, msPerBeat),
                                GetConsistencyRange(otherBeatmap, otherTime.GetValueOrDefault(), msPerBeat));
            else
                return 2;
        }
    }
}
