using MapsetParser.objects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MapsetChecks.checks.timing
{
    public class CheckWrongSnapping : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Timing",
            Message = "Wrongly or inconsistently snapped hit objects.",
            Author = "Naxess"
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
                        "and are close enough in time to be mistaken for one another.") },

                { "Snap Count",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} 1/{1} is used once or twice, ensure this makes sense.",
                        "timestamp(s) -", "X")
                    .WithCause(
                        "The beat snap divisor a hit object is on is used less than or equal to 2 times in the same difficulty " +
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
                        "{0} 1/{1} is used 6 or less times and sounds slightly off, ensure this makes sense.",
                        "timestamp(s) -", "X")
                    .WithCause(
                        "Same as the other check, except with 6 as threshold instead.") },

                { "Minor Snap Percent",
                    new IssueTemplate(Issue.Level.Minor,
                        "{0} 1/{1} makes out 5% or less of snappings and sounds slightly off, ensure this makes sense.",
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

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            // uses a non-isolated approach in order to compare for inconsistent snappings between beatmaps in a set
            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
            {
                inconsistentPlaces = new ConcurrentBag<Tuple<double, double, Beatmap>>();

                IEnumerable<Beatmap> otherBeatmaps =
                    aBeatmapSet.beatmaps.Where(aBeatmap =>
                        aBeatmap.starRating > beatmap.starRating &&
                        aBeatmap.generalSettings.mode == beatmap.generalSettings.mode);

                Parallel.ForEach(otherBeatmaps, anOtherBeatmap => PopulateInconsistentPlaces(beatmap, anOtherBeatmap));

                foreach (double inconsistentTime in inconsistentPlaces.Select(aTuple => aTuple.Item1).Distinct())
                {
                    Beatmap otherBeatmap =
                        inconsistentPlaces
                            .Where(aTuple => aTuple.Item1 == inconsistentTime)
                            .Select(aTuple => aTuple.Item3).First();

                    double stampTime =
                        inconsistentPlaces
                            .Where(aTuple => aTuple.Item1 == inconsistentTime)
                            .Select(aTuple => aTuple.Item2).FirstOrDefault();

                    if (beatmap.GetLowestDivisor(stampTime) != 0 && beatmap.GetLowestDivisor(inconsistentTime) != 0)
                    {
                        yield return new Issue(GetTemplate("Snap Consistency"), beatmap,
                            Timestamp.Get(stampTime), beatmap.GetLowestDivisor(stampTime),
                            Timestamp.Get(inconsistentTime), beatmap.GetLowestDivisor(inconsistentTime),
                            otherBeatmap);
                    }
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
        private void PrepareRareDivisors(Beatmap aBeatmap)
        {
            // rather than calculating every single snapping, this instead just looks for potentially weird snappings
            // this makes it both more convenient, due to constraining unimportant options, and more optimized
            const int countWarning = 2;
            const int countMinor = 6;
            const double percentWarning = 0.005;
            const double percentMinor = 0.05;

            countWarningDivisors = new List<int>();
            countMinorDivisors = new List<int>();
            percentWarningDivisors = new List<int>();
            percentMinorDivisors = new List<int>();

            var edgeDivisors =
                aBeatmap.hitObjects.SelectMany(anObject =>
                    anObject.GetEdgeTimes().Select(aTime =>
                        aBeatmap.GetLowestDivisor(aTime)
                    )
                )
                .Where(aDivisor => aDivisor != 0).GroupBy(aDivisor => aDivisor).Select(aGroup =>
                    new
                    {
                        divisor = aGroup.Key,
                        count = aGroup.Count()
                    }
                );

            int divisorsTotal = edgeDivisors.Sum(aDivisorGroup => aDivisorGroup.count);

            foreach (var divisorGroup in edgeDivisors)
            {
                double precentage = divisorGroup.count / (double)divisorsTotal;

                if (divisorGroup.divisor >= 6)
                {
                    if (divisorGroup.count <= countWarning) countWarningDivisors.Add(divisorGroup.divisor);
                    else if (divisorGroup.count <= countMinor) countMinorDivisors.Add(divisorGroup.divisor);

                    if (precentage < percentWarning) percentWarningDivisors.Add(divisorGroup.divisor);
                    else if (precentage < percentMinor) percentMinorDivisors.Add(divisorGroup.divisor);
                }
            }
        }

        /// <summary> Populates the warnings and minor issues for rare divisors in the given beatmap. </summary>
        private void PopulateRareDivisors(Beatmap aBeatmap)
        {
            countWarningStamps = new List<Tuple<int, string>>();
            countMinorStamps = new List<Tuple<int, string>>();
            percentWarningStamps = new List<Tuple<int, string>>();
            percentMinorStamps = new List<Tuple<int, string>>();

            foreach (HitObject hitObject in aBeatmap.hitObjects)
                foreach (double edgeTime in hitObject.GetEdgeTimes())
                    TryAddDivisorIssue(edgeTime, aBeatmap);

            countWarningStamps = countWarningStamps.Distinct().ToList();
            countMinorStamps = countMinorStamps.Distinct().ToList();
            percentWarningStamps = percentWarningStamps.Distinct().ToList();
            percentMinorStamps = percentMinorStamps.Distinct().ToList();
        }

        /// <summary> Supplied with the divisor issue list, this function basically just turns them into readable issues
        /// which we can then return in GetIssues. </summary>
        private IEnumerable<Issue> GetDivisorIssues(Beatmap aBeatmap, List<Tuple<int, string>> aDivisorTupleList, string aTemplateKey)
        {
            if (aDivisorTupleList.Count > 0)
            {
                foreach (int divisor in aDivisorTupleList.Select(aStamp => aStamp.Item1).Distinct())
                {
                    IEnumerable<string> stamps = aDivisorTupleList.Where(aStamp => aStamp.Item1 == divisor).Select(aStamp => aStamp.Item2);

                    yield return new Issue(GetTemplate(aTemplateKey), aBeatmap,
                        String.Join(" ", stamps), divisor);
                }
            }
        }

        /// <summary> Adds the given time value as a divisor issue if its divisor is really rare in the beatmap, and it isn't unsnapped. </summary>
        private void TryAddDivisorIssue(double aTime, Beatmap aBeatmap)
        {
            // no need to double error, unsnap check will take care of this
            if (aBeatmap.GetUnsnapIssue(aTime) == null)
            {
                int divisor = aBeatmap.GetLowestDivisor(aTime);

                if (countWarningDivisors  .Contains(divisor) ||
                    percentWarningDivisors.Contains(divisor) ||
                    countMinorDivisors    .Contains(divisor) ||
                    percentMinorDivisors  .Contains(divisor))
                {
                    if      (countWarningDivisors.Contains(divisor))    countWarningStamps  .Add(new Tuple<int, string>(divisor, Timestamp.Get(aTime)));
                    else if (percentWarningDivisors.Contains(divisor))  percentWarningStamps.Add(new Tuple<int, string>(divisor, Timestamp.Get(aTime)));
                    else if (countMinorDivisors.Contains(divisor))      countMinorStamps    .Add(new Tuple<int, string>(divisor, Timestamp.Get(aTime)));
                    else if (percentMinorDivisors.Contains(divisor))    percentMinorStamps  .Add(new Tuple<int, string>(divisor, Timestamp.Get(aTime)));
                }
            }
        }

        /// <summary> Populates the inconsistent places list, which keeps track of any
        /// time values in either beatmap that has no corresponding value in the other. </summary>
        private void PopulateInconsistentPlaces(Beatmap aBeatmap, Beatmap anOtherBeatmap)
        {
            List<double> timeDifferences = GetTimeDifferences(aBeatmap, anOtherBeatmap).ToList();
            List<double> otherTimeDifferences = GetTimeDifferences(anOtherBeatmap, aBeatmap).ToList();

            foreach (double time in timeDifferences)
                TryAddInconsistentPlace(otherTimeDifferences, anOtherBeatmap, time);
        }

        /// <summary> Returns any time value from the first beatmap that has no corresponding
        /// object within 3 ms in the other beatmap. </summary>
        private IEnumerable<double> GetTimeDifferences(Beatmap aBeatmap, Beatmap anOtherBeatmap)
        {
            foreach (HitObject hitObject in aBeatmap.hitObjects)
            {
                foreach (double edgeTime in hitObject.GetEdgeTimes())
                {
                    bool isOverlapping = false;

                    foreach (HitObject otherHitObject in anOtherBeatmap.hitObjects)
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
        private void TryAddInconsistentPlace(List<double> aTimeDifferences, Beatmap anOtherBeatmap, double anOtherTime)
        {
            List<double> inconsistencies = aTimeDifferences.Where(aTime =>
            {
                if (Math.Abs(aTime - anOtherTime) >= 3)
                {
                    UninheritedLine line = anOtherBeatmap.GetTimingLine<UninheritedLine>(aTime);
                    double msPerBeat = line.msPerBeat;

                    if (Math.Abs(aTime - anOtherTime) < msPerBeat)
                    {
                        double consistencyRange = GetConsistencyRange(anOtherBeatmap, aTime, msPerBeat, anOtherTime);
                        return
                            aTime + consistencyRange > anOtherTime &&
                            aTime - consistencyRange < anOtherTime;
                    }
                }

                return false;
            }).ToList();

            foreach (double inconsistency in inconsistencies)
                if (!inconsistentPlaces.Any(aPlace => aPlace.Item1 == inconsistency))
                    inconsistentPlaces.Add(new Tuple<double, double, Beatmap>(inconsistency, anOtherTime, anOtherBeatmap));
        }

        private readonly int[] divisors = new int[] { 1, 2, 3, 4, 6, 8, 12, 16 };

        /// <summary> Gets a time offset from a given divisor which may be confused with it. Larger for smaller divisors.
        /// So the offset for a 1/1 would be larger than for a 1/6, for example. If two times are supplied, the largest divisor is used. </summary>
        private double GetConsistencyRange(Beatmap anOtherBeatmap, double aTime, double aMsPerBeat, double? anOtherTime = null)
        {
            int divisor = Math.Max(anOtherBeatmap.GetLowestDivisor(aTime), 2);

            if (anOtherTime == null)
            {
                int index =
                    divisor == 0 ? -1 :
                    Array.IndexOf(divisors, divisor) == -1 ? divisors.Length :
                    Array.IndexOf(divisors, divisor);

                if (index != -1)
                {
                    double range = aMsPerBeat / divisors.ElementAt(index + 2 > divisors.Length - 1 ? divisors.Length - 1 : index + 2) - 2;

                    return range;
                }
                else
                    // if the divisor is 0, that means the time we gave it was unsnapped
                    return 2;
            }

            int otherDivisor = Math.Max(anOtherBeatmap.GetLowestDivisor(anOtherTime.GetValueOrDefault()), 2);

            // should the difficulty range be too large, we only point it out if the lower difficulty has a higher divisor
            // (since higher diffs having higher divisors is normal)
            if (otherDivisor > divisor)
                return Math.Max(GetConsistencyRange(anOtherBeatmap, aTime, aMsPerBeat),
                                GetConsistencyRange(anOtherBeatmap, anOtherTime.GetValueOrDefault(), aMsPerBeat));
            else
                return 2;
        }
    }
}
