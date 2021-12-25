using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System.Collections.Generic;
using System.Numerics;

namespace MapsetChecks.Checks.Standard.Compose
{
    [Check]
    public class CheckAmbiguity : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Modes = new[]
            {
                Beatmap.Mode.Standard
            },
            Category = "Compose",
            Message = "Ambiguous slider intersection.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Preventing sliders from being excessively difficult, or even impossible, to read in gameplay.
                    <image-right>
                        https://i.imgur.com/Y3TB2m7.png
                        A slider with a 3-way intersection in the middle. Considered readable if and only if the 
                        middle section goes up to the left and down on the right.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Giving the player little to no hints as to how to move their cursor through the slider makes for an 
                    unfair gameplay experience. This means the majority of the difficulty does not stem from how well the 
                    player can click on and move through the slider, but from how well they can guess how to move through 
                    it. If implemented well, however, it is possible for the player to learn how to move through the 
                    sliders before they are able to fail from guessing wrong.
                    <br \><br \>
                    Particularly slow sliders, for instance, may move their follow circle slow enough for players to correct 
                    themselves if they guessed wrong, whereas fast sliders often do not include as many slider ticks and are 
                    as such more lenient. Sliders that do not require that the player move their cursor are also hard to fail 
                    from guessing wrong since there it often doesn't matter if they know how to position their cursor.
                    <image-right>
                        https://i.imgur.com/LPVmy81.png
                        Two sliders which are practically the same in gameplay. The left one has a much higher chance of 
                        players guessing wrong on due to the tail not being visible.
                    </image>"
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Slider edges are almost perfectly overlapping.",
                        "timestamp - ")
                    .WithCause(
                        "The edges of a slider curve are 5 px or less apart, and a slider tick is 2 circle radii from the head.") },

                { "Anchor",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} {1} and red anchor overlap is possibly ambigious.",
                        "timestamp - ", "Head/tail")
                    .WithCause(
                        "The head or tail of a slider is a distance of 10 px or less to a red node, having been more than 30 px away " +
                        "at a point in time between the two.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (var hitObject in beatmap.hitObjects)
            {
                if (!(hitObject is Slider slider))
                    continue;

                Vector2 tailPosition = slider.GetPathPosition(slider.time + slider.GetCurveDuration());
                float curveEdgesDistance = Vector2.Distance(slider.Position, tailPosition);

                if (curveEdgesDistance <= 5 && CouldSliderBreak(slider))
                    yield return new Issue(GetTemplate("Warning"), beatmap,
                        Timestamp.Get(hitObject));

                var anchorPositions = slider.redAnchorPositions;
                if (slider.curveType == Slider.CurveType.Linear)
                    anchorPositions = slider.nodePositions;

                // Anchors only exist for bezier sliders (red nodes) and linear sliders (where any node acts as a red node).
                if (slider.curveType != Slider.CurveType.Bezier &&
                    slider.curveType != Slider.CurveType.Linear ||
                    anchorPositions.Count == 0)
                {
                    continue;
                }
                
                var prevAnchorPosition = anchorPositions[0];
                double curDistance = 0;
                double totalDistance = 0;

                for (int i = 1; i < anchorPositions.Count; ++i)
                    totalDistance += Vector2.Distance(anchorPositions[i - 1], anchorPositions[i]);

                foreach(var anchorPosition in anchorPositions)
                {
                    float prevAnchorDistance = Vector2.Distance(anchorPosition, prevAnchorPosition);
                    curDistance += prevAnchorDistance;

                    float? headAnchorDistance = null;
                    float? tailAnchorDistance = null;

                    // We only consider ones over 60 px apart since things like zig-zag patterns would otherwise be false-positives.
                    if (curDistance > 60)
                        headAnchorDistance = Vector2.Distance(slider.Position, anchorPosition);

                    if (totalDistance - curDistance > 60)
                        tailAnchorDistance = Vector2.Distance(anchorPosition, tailPosition);

                    if (headAnchorDistance <= 5)
                    {
                        yield return new Issue(GetTemplate("Anchor"), beatmap,
                            Timestamp.Get(hitObject), "Head");
                        break;
                    }

                    if (tailAnchorDistance <= 5)
                    {
                        yield return new Issue(GetTemplate("Anchor"), beatmap,
                            Timestamp.Get(hitObject), "Tail");
                        break;
                    }

                    prevAnchorPosition = anchorPosition;
                }
            }
        }

        private static bool CouldSliderBreak(Slider slider) =>
            MaxDistanceFromHead(slider) - slider.beatmap.difficultySettings.GetCircleRadius() * 2 > 0;

        private static float MaxDistanceFromHead(Slider slider)
        {
            float maxDistance = 0;
            foreach (double tickTime in slider.GetSliderTickTimes())
            {
                Vector2 tickPosition = slider.GetPathPosition(tickTime);
                float distance = (tickPosition - slider.Position).Length();
                if (maxDistance < distance)
                    maxDistance = distance;
            }
            return maxDistance;
        }
    }
}
