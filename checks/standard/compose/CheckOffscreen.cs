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
using System.Numerics;

namespace MapsetChecks.checks.standard.compose
{
    public class CheckOffscreen : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                Beatmap.Mode.Standard
            },
            Category = "Compose",
            Message = "Offscreen hit objects.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing the border of hit objects from even partially becoming offscreen in 4:3 aspect ratios.
                    <note>
                        4:3 includes 16:9 and 16:10, the only difference is the width, so you can check for offscreens 
                        along the top and bottom with 16:9 and 16:10 and it will be the same as what you'd see with 4:3.
                    </note>
                    <image-right>
                        https://i.imgur.com/zXT4Zwr.png
                        A slider end which is partially offscreen along the bottom of the screen.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Although everything is technically readable and playable if an object is only partially offscreen, 
                    it trip up players using relative movement input (for example mouse) when their cursor hits the 
                    side of the screen, since the game will offset the cursor back into the screen which is difficult 
                    to correct while in the middle of gameplay.
                    <br \><br \>
                    Since objects partially offscreen also have a smaller area to hit, if not hitting the screen 
                    causing the problems above, it makes those objects need more precision to play which isn't 
                    consistent with how the rest of the game works, especially considering that the punishment for 
                    overshooting is getting your cursor offset slightly but still hitting the object and not missing 
                    like you probably would otherwise."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Offscreen",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} {1} is offscreen.",
                        "timestamp - ", "object")
                    .WithCause(
                        "The border of a hit object is partially off the screen in 4:3 aspect ratios.") },

                { "Prevented",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} {1} would be offscreen by {2} px, but the game prevents it.",
                        "timestamp - ", "object", "osu!pixels")
                    .WithCause(
                        "The .osu code implies the hit object is in a place where it would be off the 512x512 playfield area, but the game has " +
                        "moved it back inside the screen automatically.") },

                { "Bezier Margin",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Slider body is possibly offscreen, ensure the entire white border is visible on a 4:3 aspect ratio.",
                        "timestamp - ")
                    .WithCause(
                        "The slider body of a bezier slider is approximated to be 1 osu!pixel away from being offscreen at some point on its curve.") }
            };
        }

        // Old measurements: -60, 430, -66, 578
        // New measurements: -60, 428, -67, 579 (tested with slider tails)
        private const int UPPER_LIMIT = -60;
        private const int LOWER_LIMIT = 428;
        private const int LEFT_LIMIT  = -67;
        private const int RIGHT_LIMIT = 579;

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            foreach (HitObject hitObject in aBeatmap.hitObjects)
            {
                string type = hitObject is Circle ? "Circle" : "Slider head";
                if (hitObject is Circle || hitObject is Slider)
                {
                    float circleRadius = aBeatmap.difficultySettings.GetCircleRadius();

                    if (hitObject.Position.Y + circleRadius > 428)
                        yield return new Issue(GetTemplate("Offscreen"), aBeatmap,
                            Timestamp.Get(hitObject), type);

                    // The game prevents the head of objects from going offscreen inside a 512 by 512 px square,
                    // meaning heads can still go offscreen at the bottom due to how aspect ratios work.
                    else if (GetOffscreenBy(hitObject.Position, aBeatmap) > 0)
                        yield return new Issue(GetTemplate("Prevented"), aBeatmap,
                            Timestamp.Get(hitObject), type);
                    
                    if (hitObject is Slider slider)
                    {
                        if (GetOffscreenBy(slider.EndPosition, aBeatmap) > 0)
                            yield return new Issue(GetTemplate("Offscreen"), aBeatmap,
                                Timestamp.Get(hitObject.GetEndTime()), "Slider tail");
                        else
                        {
                            bool offscreenBodyFound = false;
                            foreach(Vector2 pathPosition in slider.pathPxPositions)
                            {
                                if (GetOffscreenBy(pathPosition, aBeatmap) > 0)
                                {
                                    yield return new Issue(GetTemplate("Offscreen"), aBeatmap,
                                        Timestamp.Get(hitObject), "Slider body");

                                    offscreenBodyFound = true;
                                    break;
                                }
                            }

                            // Since we sample parts of slider bodies, and these aren't math formulas (although they could be),
                            // we'd need to sample an infinite amount of points on the path, which is too intensive, so instead
                            // we approximate and apply leniency to ensure false-positive over false-negative.
                            if (!offscreenBodyFound)
                            {
                                foreach (Vector2 pathPosition in slider.pathPxPositions)
                                {
                                    Vector2 exactPathPosition = pathPosition;
                                    if (GetOffscreenBy(exactPathPosition, aBeatmap, 2) > 0 && slider.curveType != Slider.CurveType.Linear)
                                    {
                                        bool isOffscreen = false;
                                        for (int j = 0; j < slider.GetCurveDuration() * 50; ++j)
                                        {
                                            exactPathPosition = slider.GetPathPosition(slider.time + j / 50d);

                                            double offscreenBy = GetOffscreenBy(exactPathPosition, aBeatmap);
                                            if (offscreenBy > 0)
                                                isOffscreen = true;
                                        }

                                        if (isOffscreen)
                                            yield return new Issue(GetTemplate("Offscreen"), aBeatmap,
                                                Timestamp.Get(hitObject), "Slider body");
                                        else
                                            yield return new Issue(GetTemplate("Bezier Margin"), aBeatmap,
                                                Timestamp.Get(hitObject));

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary> Returns how far offscreen an object is in pixels (in-game pixels, not resolution). </summary>
        private float GetOffscreenBy(Vector2 aPoint, Beatmap aBeatmap, float aLeniency = 0)
        {
            float circleRadius = aBeatmap.difficultySettings.GetCircleRadius();

            float offscreenBy = 0;

            float offscreenRight = aPoint.X + circleRadius - RIGHT_LIMIT + aLeniency;
            float offscreenLeft  = circleRadius - aPoint.X + LEFT_LIMIT  + aLeniency;
            float offscreenLower = aPoint.Y + circleRadius - LOWER_LIMIT + aLeniency;
            float offscreenUpper = circleRadius - aPoint.Y + UPPER_LIMIT + aLeniency;

            if (offscreenRight > offscreenBy) offscreenBy = offscreenRight;
            if (offscreenLeft  > offscreenBy) offscreenBy = offscreenLeft;
            if (offscreenLower > offscreenBy) offscreenBy = offscreenLower;
            if (offscreenUpper > offscreenBy) offscreenBy = offscreenUpper;

            return (float)Math.Ceiling(offscreenBy * 100) / 100f;
        }
    }
}
