using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace MapsetChecks.checks.standard.compose
{
    [Check]
    public class CheckBurai : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                Beatmap.Mode.Standard
            },
            Category = "Compose",
            Message = "Burai slider.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing sliders from being excessively difficult, or even impossible, to read in gameplay.
                    <image-right>
                        https://i.imgur.com/fMa1hWR.png
                        A slider which may be considered readable if it goes right, up, and then down, but would 
                        mean going through the same slider path twice for the right and top parts.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Largely follows the same reasoning as the overlapping slider tail/head/anchor check; if the player 
                    needs to rely on guessing, and guessing wrong results in a slider break, then that's an unfair 
                    gameplay experience.
                    <br \><br \>
                    A single path going back on itself far enough for the follow circle to not cover everything, is 
                    neither lenient nor something players will expect considering that sliders are expected to have 
                    a clear path. When the follow circle does cover the whole path, however, it's generally acceptable 
                    since even if the player misreads it, it usually doesn't cause any slider breaks.
                    <br \><br \>
                    Should a slider go back on itself and end before it creates its own borders, players without slider 
                    tails enabled will have a hard time seeing how far back into itself it goes, or even if it goes 
                    back on itself at all.
                    <image-right>
                        https://i.imgur.com/StRTQzZ.png
                        A slider which goes back on itself multiple times and is impossible to read for players with 
                        slider tails hidden, which is common in skinning.
                    </image>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Definitely",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Burai.",
                        "timestamp - ")
                    .WithCause(
                        "The burai score of a slider shape, based on the distance and delta angle between intersecting parts of " +
                        "the curve, is very high.") },

                { "Potentially",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Potentially burai.",
                        "timestamp - ")
                    .WithCause(
                        "Same as the other check, but with a lower score threshold.") }
            };
        }
        
        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (HitObject hitObject in beatmap.hitObjects)
            {
                if (!(hitObject is Slider slider) || slider.curveType != Slider.CurveType.Bezier)
                    continue;

                // Make sure the path doesn't go back on itself (basically the angle shouldn't be too similar
                // between intersecting parts of a slider). Only checks sections of a slider that have some
                // distance in time between each other, allowing very small burai-like structure, which is
                // usually readable.
                    
                double angleIntersect;
                double otherAngleIntersect;
                double diffAngleIntersect;

                bool passedMargin;
                    
                double distance;
                double maxDistance = 3;
                    
                List<double> buraiScores = new List<double>();

                for (int i = 1; i < slider.pathPxPositions.Count; ++i)
                {
                    passedMargin = false;

                    // Only check places we haven't been yet for optimization.
                    for (int j = i + 1; j < slider.pathPxPositions.Count - 1; ++j)
                    {
                        distance = GetDistance(slider.pathPxPositions[i], slider.pathPxPositions[j]);
                            
                        // First ensure the point is far enough away to not be a small burai structure.
                        if (!passedMargin && distance >= maxDistance)
                            passedMargin = true;

                        // Then if it returns, we know the slider is intersecting itself.
                        if (passedMargin && distance >= maxDistance)
                            continue;

                        angleIntersect = GetAngle(slider.pathPxPositions[i - 1], slider.pathPxPositions[i]);
                        otherAngleIntersect = GetAngle(slider.pathPxPositions[j], slider.pathPxPositions[j + 1]);
                                
                        // Compare the intersection angles, resets after 180 degrees since we're comparing tangents.
                        diffAngleIntersect = Math.Abs(WrapAngle(angleIntersect - otherAngleIntersect, 0.5));
                                
                        double distanceScore = 100 * Math.Sqrt(10) / Math.Pow(10, 2 * distance) / 125;
                        double angleScore = 1 / (Math.Pow(diffAngleIntersect / Math.PI * 20, 3) + 0.01) / 250;

                        buraiScores.Add(angleScore * distanceScore);
                    }
                }

                double totalBuraiScore = GetWeighedScore(buraiScores);
                if (totalBuraiScore <= 0)
                    continue;

                // Note that this may false positive in places with slight but readable overlapping curves.
                if (totalBuraiScore > 5)
                    yield return new Issue(GetTemplate("Definitely"), beatmap,
                        Timestamp.Get(hitObject));

                else if (totalBuraiScore > 2)
                    yield return new Issue(GetTemplate("Potentially"), beatmap,
                        Timestamp.Get(hitObject));
            }
        }

        /// <summary> Returns the smallest angle in radians. </summary>
        private double WrapAngle(double radians, double scale = 1)
        {
            return radians > Math.PI * scale ? Math.PI * 2 * scale - radians : radians;
        }

        /// <summary> Returns the angle between two 2D vectors, a value between 0 and 2 PI. </summary>
        private double GetAngle(Vector2 vector, Vector2 otherVector, double wrapScale = 1)
        {
            double radians =
                WrapAngle(
                    Math.Atan2(
                        vector.Y - otherVector.Y,
                        vector.X - otherVector.X),
                    wrapScale);
            
            return (radians >= 0 ? radians : Math.PI * 2 + radians) % Math.PI;
        }

        /// <summary> Returns the euclidean distance between two 2D vectors. </summary>
        private double GetDistance(Vector2 vector, Vector2 otherVector)
        {
            return
                Math.Sqrt(
                    Math.Pow(vector.X - otherVector.X, 2) +
                    Math.Pow(vector.Y - otherVector.Y, 2));
        }

        /// <summary> Returns the weighted score of burai scores, decaying by 90% for each lower number. </summary>
        private double GetWeighedScore(List<double> buraiScores)
        {
            double score = 0;

            // Sort by highest impact and then each following is worth less.
            List<double> sortedScores = buraiScores.OrderByDescending(num => num).ToList();
            for (int i = 0; i < sortedScores.Count; ++i)
                score += sortedScores[i] * Math.Pow(0.9, i);

            return score;
        }
    }
}
