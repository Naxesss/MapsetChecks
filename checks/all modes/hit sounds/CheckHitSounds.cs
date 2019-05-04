using MapsetChecks.objects;
using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.hit_sounds
{
    public class CheckHitSounds : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                // This check would take on another meaning if applied to taiko, since there you basically map with hit sounds
                Beatmap.Mode.Standard,
                Beatmap.Mode.Catch,
                Beatmap.Mode.Mania
            },
            Category = "Hit Sounds",
            Message = "Long periods without hit sounding.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "No Hit Sounds",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "This beatmap contains no hit sounds or sampleset changes.")
                    .WithCause(
                        "There are no hit sounds or sampleset changes anywhere in a difficulty.") },

                { "Unrankable",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} No hit sounds or sampleset changes from here to {1}, ({2} s).",
                        "timestamp - ", "timestamp - ", "duration")
                    .WithCause(
                        "The hit sound score value, based on the amount of hit objects and time between two points without hit sounds " +
                        "or sampleset changes, is way too low.") },

                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} No hit sounds or sampleset changes from here to {1}, ({2} s).",
                        "timestamp - ", "timestamp - ", "duration")
                    .WithCause(
                        "Same as the other check, but with a threshold which is higher, but still very low.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            double prevTime = aBeatmap.hitObjects.Count > 0 ? aBeatmap.hitObjects.First().time : 0;
            int objectsPassed = 0;
            int totalHitSounds = 0;

            Beatmap.Sampleset? prevSample = null;

            List<Issue> issues = new List<Issue>();
            
            void ApplyFeedbackUpdate(HitObject.HitSound aHitSound, Beatmap.Sampleset aSampleSet, HitObject hitObject, double aTime)
            {
                if (aHitSound > 0 ||
                    aSampleSet != prevSample && prevSample != null ||
                    aBeatmap.generalSettings.mode == Beatmap.Mode.Mania && hitObject.filename != "")
                {
                    prevSample = aSampleSet;

                    ++totalHitSounds;
                    Issue issue = GetIssueFromUpdate(aTime, ref objectsPassed, ref prevTime, aBeatmap);
                    if (issue != null)
                        issues.Add(issue);
                }
                else
                    ++objectsPassed;
            }

            foreach (HitObject hitObject in aBeatmap.hitObjects)
            {
                while (true)
                {
                    // Breaks and spinners don't really need to be hit sounded so we take that into account
                    // by looking for any between the current object and excluding their drain time if present.
                    Break @break =
                        aBeatmap.breaks.FirstOrDefault(aBreak =>
                            aBreak.endTime > prevTime &&
                            aBreak.endTime < hitObject.time);
                    Spinner spinner =
                        aBeatmap.hitObjects.OfType<Spinner>().FirstOrDefault(aSpinner =>
                            aSpinner.endTime > prevTime &&
                            aSpinner.endTime < hitObject.time);
                    
                    double excludeStart =
                        @break == null ?
                            spinner == null ?
                                -1 :
                                spinner.time :
                            @break.time;

                    double excludeEnd =
                        @break == null ?
                            spinner == null ?
                                -1 :
                                spinner.endTime :
                            @break.endTime;

                    if (@break != null && spinner != null)
                    {
                        excludeStart = @break.time > spinner.time ? @break.time : spinner.time;
                        excludeEnd = @break.endTime > spinner.endTime ? @break.endTime : spinner.endTime;
                    }
                    else if (@break == null && spinner == null)
                        break;

                    HitObject objectBeforeExcl = aBeatmap.GetHitObject(excludeStart - 1);
                    HitObject objectAfterExcl = aBeatmap.GetNextHitObject(excludeEnd);

                    double endTimeBeforeExcl =
                        objectBeforeExcl is Spinner ? ((Spinner)objectBeforeExcl).endTime
                        : objectBeforeExcl is Slider ? ((Slider)objectBeforeExcl).endTime
                        : objectBeforeExcl.time;

                    // Between the previous object's time and the end time before the exclusion,
                    // storyboarded hit sounds should be accounted for in mania, since they need to
                    // use them as substitutes to actual hit sounding.
                    foreach (Issue storyIssue in GetStoryHsIssuesFromUpdates(aBeatmap, prevTime, endTimeBeforeExcl, ref objectsPassed, ref prevTime))
                        issues.Add(storyIssue);
                    
                    // Exclusion happens through updating prevTime manually rather than through the update function.
                    Issue issue = GetIssueFromUpdate(endTimeBeforeExcl, ref objectsPassed, ref prevTime, aBeatmap);
                    if (issue != null)
                        issues.Add(issue);
                    prevTime = objectAfterExcl.time;
                }
                
                // Regardless of there being a spinner or break, storyboarded hit sounds should still be taken into account.
                foreach (Issue storyIssue in GetStoryHsIssuesFromUpdates(aBeatmap, prevTime, hitObject.time, ref objectsPassed, ref prevTime))
                    issues.Add(storyIssue);

                if (hitObject is Circle)
                    ApplyFeedbackUpdate(hitObject.hitSound, hitObject.GetSampleset(), hitObject, hitObject.time);

                if (hitObject is Slider slider)
                {
                    ApplyFeedbackUpdate(slider.startHitSound, slider.GetStartSampleset(), slider, slider.time);

                    if (slider.repeatHitSounds.Count() > 0)
                        for (int repeatIndex = 0; repeatIndex < slider.edgeAmount - 1; ++repeatIndex)
                            ApplyFeedbackUpdate(
                                slider.repeatHitSounds.ElementAt(repeatIndex),
                                slider.GetRepeatSampleset(repeatIndex),
                                slider,
                                Math.Floor(slider.time + slider.GetCurveDuration() * (repeatIndex + 1)));

                    ApplyFeedbackUpdate(slider.endHitSound, slider.GetEndSampleset(), slider, slider.endTime);
                }
            }
            
            if (totalHitSounds == 0)
                yield return new Issue(GetTemplate("No Hit Sounds"), aBeatmap);
            else
                foreach (Issue issue in issues)
                    yield return issue;
        }

        /// <summary> Returns an issue when too much time and/or too many objects were passed before this method was called again. </summary>
        private Issue GetIssueFromUpdate(double aCurrentTime, ref int anObjectsPassed, ref double aPreviousTime, Beatmap aBeatmap)
        {
            double prevTime = aPreviousTime;

            double timeDifference = aCurrentTime - prevTime;
            double objectDifference = anObjectsPassed;
            
            anObjectsPassed = 0;
            aPreviousTime = aCurrentTime;

            double timeRatio = timeDifference;
            double objectRatio = objectDifference * 200;

            // Thresholds
            int warningTotal    = 10000;
            int warningTime     = 8 * 1500;     // 12 seconds (8 measures of 160 BPM, usually makes up a whole section in the song)
            int warningObject   = 2 * 200;      // 2 objects (difficulty invariant, so needs to work for easy diffs too)

            int unrankableTotal     = 30000;
            int unrankableTime      = 24 * 1500;    // 36 seconds (24 measures of 160 BPM, usually makes up multiple sections in the song)
            int unrankableObject    = 8 * 200;      // 8 objects
            
            if (timeRatio + objectRatio > unrankableTotal &&    // at least this much of the combined
                timeRatio > unrankableTime &&                   // at least this much of the individual
                objectRatio > unrankableObject)
            {
                return new Issue(GetTemplate("Unrankable"), aBeatmap,
                    Timestamp.Get(aCurrentTime - timeDifference), Timestamp.Get(aCurrentTime),
                    (Math.Round(timeDifference / 10) / 100).ToString(CultureInfo.InvariantCulture));
            }

            else if (
                timeRatio + objectRatio > warningTotal &&
                timeRatio > warningTime &&
                objectRatio > warningObject)
            {
                return new Issue(GetTemplate("Warning"), aBeatmap,
                    Timestamp.Get(aCurrentTime - timeDifference), Timestamp.Get(aCurrentTime),
                    (Math.Round(timeDifference / 10) / 100).ToString(CultureInfo.InvariantCulture));
            }
            
            return null;
        }

        /// <summary> Returns issues for every storyboarded hit sound where too much time and/or too many objects were passed since last update. </summary>
        private List<Issue> GetStoryHsIssuesFromUpdates(Beatmap aBeatmap, double aStartTime, double anEndTime, ref int anObjectsPassed, ref double aPrevTime)
        {
            List<Issue> issues = new List<Issue>();
            if (aBeatmap.generalSettings.mode == Beatmap.Mode.Mania)
            {
                while (true)
                {
                    StoryHitSound storyHitSound = aBeatmap.storyHitSounds.FirstOrDefault(
                        aHitsound => aHitsound.time > aStartTime && aHitsound.time < anEndTime);

                    if (storyHitSound == null)
                        break;

                    Issue maniaIssue = GetIssueFromUpdate(storyHitSound.time, ref anObjectsPassed, ref aPrevTime, aBeatmap);
                    if (maniaIssue != null)
                        issues.Add(maniaIssue);
                }
            }
            return issues;
        }
    }
}
