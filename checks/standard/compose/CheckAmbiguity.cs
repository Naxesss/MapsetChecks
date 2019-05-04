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

namespace MapsetChecks.checks.timing
{
    public class CheckAmbiguity : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                Beatmap.Mode.Standard
            },
            Category = "Compose",
            Message = "Perfectly overlapping combination of tail, head or red anchors.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Unrankable",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} Tail and head are perfectly overlapping.",
                        "timestamp - ")
                    .WithCause(
                        "The start and end of a slider are a distance of 2 px or less away from each other.") },

                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Tail and head are almost perfectly overlapping.",
                        "timestamp - ")
                    .WithCause(
                        "Same as the other check, except 5 px or less instead.") },

                { "Anchor",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} {1} and red anchor overlap is possibly ambigious.",
                        "timestamp - ", "Head/tail")
                    .WithCause(
                        "The head or tail of a slider is a distance of 10 px or less to a red node, having been more than 30 px away " +
                        "at a point in time between the two.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            foreach (HitObject hitObject in aBeatmap.hitObjects)
            {
                if (hitObject is Slider slider)
                {
                    Vector2 tailPosition = slider.GetPathPosition(slider.time + slider.GetCurveDuration());
                    float headTailDistance = Vector2.Distance(slider.Position, tailPosition);

                    if (headTailDistance <= 2)
                        yield return new Issue(GetTemplate("Unrankable"), aBeatmap,
                            Timestamp.Get(hitObject));

                    else if (headTailDistance <= 5)
                        yield return new Issue(GetTemplate("Warning"), aBeatmap,
                            Timestamp.Get(hitObject));

                    List<Vector2> anchorPositions = slider.redAnchorPositions;
                    if (slider.curveType == Slider.CurveType.Linear)
                        anchorPositions = slider.nodePositions;

                    // Anchors only exist for bezier sliders (red nodes) and linear sliders (where any node acts as a red node).
                    if ((slider.curveType == Slider.CurveType.Bezier ||
                        slider.curveType == Slider.CurveType.Linear) &&
                        anchorPositions.Count > 0)
                    {
                        Vector2 prevAnchorPosition = anchorPositions[0];
                        double curDistance = 0;
                        double totalDistance = 0;

                        for (int i = 1; i < anchorPositions.Count; ++i)
                            totalDistance += Vector2.Distance(anchorPositions[i - 1], anchorPositions[i]);

                        foreach(Vector2 anchorPosition in anchorPositions)
                        {
                            float prevAnchorDistance = Vector2.Distance(anchorPosition, prevAnchorPosition);
                            curDistance += prevAnchorDistance;

                            float? headAnchorDistance = null;
                            float? tailAnchorDistance = null;

                            // We only consider ones over 30 px apart since things like zig-zag patterns would otherwise be false-positives.
                            if (curDistance > 30)
                                headAnchorDistance = Vector2.Distance(slider.Position, anchorPosition);

                            if (totalDistance - curDistance > 30)
                                tailAnchorDistance = Vector2.Distance(anchorPosition, tailPosition);

                            if (headAnchorDistance != null && headAnchorDistance <= 10)
                                yield return new Issue(GetTemplate("Anchor"), aBeatmap,
                                    Timestamp.Get(hitObject), "Head");

                            if (tailAnchorDistance != null && tailAnchorDistance <= 10)
                                yield return new Issue(GetTemplate("Anchor"), aBeatmap,
                                    Timestamp.Get(hitObject), "Tail");

                            prevAnchorPosition = anchorPosition;
                        }
                    }
                }
            }
        }
    }
}
