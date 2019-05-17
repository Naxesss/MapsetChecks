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
using System.Numerics;

namespace MapsetChecks.checks.timing
{
    public class CheckObscuredRepeat : BeatmapCheck
    {
        private const float CLOSE_REPEAT     = 15;
        private const float TOO_CLOSE_REPEAT = 4;

        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                Beatmap.Mode.Standard
            },
            Difficulties = new Beatmap.Difficulty[]
            {
                Beatmap.Difficulty.Easy,
                Beatmap.Difficulty.Normal,
                Beatmap.Difficulty.Hard,
                Beatmap.Difficulty.Insane
            },
            Category = "Compose",
            Message = "Obscured repeat arrows.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Obscured",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Reverse arrow {1} obscured.",
                        "timestamp - ", "(potentially)")
                    .WithCause(
                        "An object before a reverse arrow ends over where it appears close in time.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            // Represents the duration the repeat arrow is fully opaque.
            double opaqueTime = aBeatmap.difficultySettings.GetPreemptTime();

            foreach (HitObject hitObject in aBeatmap.hitObjects)
            {
                if (hitObject is Slider slider && slider.edgeAmount > 1)
                {
                    double repeatTime = slider.time + slider.GetCurveDuration();
                    Vector2 reversePosition = slider.GetPathPosition(repeatTime);

                    List<HitObject> selectedObjects = new List<HitObject>();
                    bool isSerious = false;
                    
                    IEnumerable<HitObject> hitObjectsRightBeforeRepeat =
                        aBeatmap.hitObjects.Where(anObject =>
                            anObject.GetEndTime() > repeatTime - opaqueTime &&
                            anObject.GetEndTime() < repeatTime);

                    foreach (HitObject otherHitObject in hitObjectsRightBeforeRepeat)
                    {
                        float distanceToRepeat;
                        if (otherHitObject is Slider otherSlider)
                            distanceToRepeat =
                                (float)Math.Sqrt(
                                    Math.Pow(otherSlider.EndPosition.X - reversePosition.X, 2) +
                                    Math.Pow(otherSlider.EndPosition.Y - reversePosition.Y, 2));
                        else
                            distanceToRepeat =
                                (float)Math.Sqrt(
                                    Math.Pow(otherHitObject.Position.X - reversePosition.X, 2) +
                                    Math.Pow(otherHitObject.Position.Y - reversePosition.Y, 2));

                        if (distanceToRepeat < TOO_CLOSE_REPEAT)
                            isSerious = true;

                        if (distanceToRepeat < CLOSE_REPEAT)
                        {
                            List<HitObject> hitObjects;
                            if (hitObject.time > otherHitObject.time)
                                hitObjects = new List<HitObject>() { otherHitObject, hitObject };
                            else
                                hitObjects = new List<HitObject>() { hitObject, otherHitObject };

                            selectedObjects.AddRange(hitObjects);
                            break;
                        }
                    }

                    if (selectedObjects.Count > 0)
                        yield return new Issue(GetTemplate("Obscured"), aBeatmap,
                            Timestamp.Get(selectedObjects.ToArray()), (isSerious ? "" : "potentially "));
                }
            }
        }
    }
}
