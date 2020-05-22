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
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing slider reverses from being covered up by other objects or combo bursts before players 
                    can react to them.
                    <image-right>
                        https://i.imgur.com/BS8BkT7.png
                        Although many skins remove combo bursts, these can hide reverses under them in the same way 
                        other objects can in gameplay, so only looking at the editor is a bit misleading.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Some mappers like to stack objects on upcoming slider ends to make everything seem more 
                    coherent, but in doing so, reverses can become obscured and impossible to read unless you know 
                    they're there. For more experienced players, however, this isn't as much of a problem since you 
                    learn to hold sliders more efficiently and can react faster."
                }
            }
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

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            int closeThreshold    = 15;
            int tooCloseThreshold = 4;

            // Represents the duration the reverse arrow is fully opaque.
            double opaqueTime = beatmap.difficultySettings.GetPreemptTime();

            foreach (HitObject hitObject in beatmap.hitObjects)
            {
                if (!(hitObject is Slider slider) || slider.edgeAmount <= 1)
                    continue;

                double reverseTime = slider.time + slider.GetCurveDuration();
                Vector2 reversePosition = slider.GetPathPosition(reverseTime);

                List<HitObject> selectedObjects = new List<HitObject>();
                bool isSerious = false;
                    
                IEnumerable<HitObject> hitObjectsRightBeforeReverse =
                    beatmap.hitObjects.Where(otherHitObject =>
                        otherHitObject.GetEndTime() > reverseTime - opaqueTime &&
                        otherHitObject.GetEndTime() < reverseTime);

                foreach (HitObject otherHitObject in hitObjectsRightBeforeReverse)
                {
                    // Spinners don't really obscure anything and are handled by recovery time anyway.
                    if (otherHitObject is Spinner)
                        continue;

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

                    if (distanceToReverse >= closeThreshold)
                        continue;

                    List<HitObject> hitObjects;
                    if (hitObject.time > otherHitObject.time)
                        hitObjects = new List<HitObject>() { otherHitObject, hitObject };
                    else
                        hitObjects = new List<HitObject>() { hitObject, otherHitObject };

                    selectedObjects.AddRange(hitObjects);
                    break;
                }

                if (selectedObjects.Count > 0)
                    yield return new Issue(GetTemplate("Obscured"), beatmap,
                        Timestamp.Get(selectedObjects.ToArray()), (isSerious ? "" : "potentially "));
            }
        }
    }
}
