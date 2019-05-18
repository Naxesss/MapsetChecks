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

namespace MapsetChecks.checks.standard.compose
{
    public class CheckObscuredReverse : BeatmapCheck
    {
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
            Message = "Obscured reverse arrows.",
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
            int closeThreshold    = 15;
            int tooCloseThreshold = 4;

            // Represents the duration the reverse arrow is fully opaque.
            double opaqueTime = aBeatmap.difficultySettings.GetPreemptTime();

            foreach (HitObject hitObject in aBeatmap.hitObjects)
            {
                if (hitObject is Slider slider && slider.edgeAmount > 1)
                {
                    double reverseTime = slider.time + slider.GetCurveDuration();
                    Vector2 reversePosition = slider.GetPathPosition(reverseTime);

                    List<HitObject> selectedObjects = new List<HitObject>();
                    bool isSerious = false;
                    
                    IEnumerable<HitObject> hitObjectsRightBeforeReverse =
                        aBeatmap.hitObjects.Where(anObject =>
                            anObject.GetEndTime() > reverseTime - opaqueTime &&
                            anObject.GetEndTime() < reverseTime);

                    foreach (HitObject otherHitObject in hitObjectsRightBeforeReverse)
                    {
                        float distanceToReverse;
                        if (otherHitObject is Slider otherSlider)
                            distanceToReverse =
                                (float)Math.Sqrt(
                                    Math.Pow(otherSlider.EndPosition.X - reversePosition.X, 2) +
                                    Math.Pow(otherSlider.EndPosition.Y - reversePosition.Y, 2));
                        else
                            distanceToReverse =
                                (float)Math.Sqrt(
                                    Math.Pow(otherHitObject.Position.X - reversePosition.X, 2) +
                                    Math.Pow(otherHitObject.Position.Y - reversePosition.Y, 2));

                        if (distanceToReverse < tooCloseThreshold)
                            isSerious = true;

                        if (distanceToReverse < closeThreshold)
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
