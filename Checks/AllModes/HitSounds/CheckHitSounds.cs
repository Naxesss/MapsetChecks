using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.hit_sounds
{
    [Check]
    public class CheckHitSounds : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                // This check would take on another meaning if applied to taiko, since there you basically map with hit sounds.
                Beatmap.Mode.Standard,
                Beatmap.Mode.Catch
            },
            Category = "Hit Sounds",
            Message = "Long periods without hit sounding.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring varied feedback is used frequently throughout the map. Not too frequently, but mixing things up at least 
                    once or twice every measure is preferable. This could be with hit sounds, sampleset changes or additions."
                },
                {
                    "Reasoning",
                    @"
                    Accenting and complementing the song with hit sounds, either by reflecting it or adding to it, generally yields 
                    better feedback than if the same sound would be used throughout. However, the option to use the same sounds for all 
                    hit sounds and samplesets is still possible through skinning on the players' end for those who prefer the monotone 
                    approach."
                },
                {
                    "Exceptions",
                    @"
                    Taiko is an exception due to relying on hit sounds for gameplay mechanics. Circles are by default don, and can be 
                    turned into kat by using a clap or whistle hit sound, for example. As such applying this check to taiko would
                    make it take on a different meaning.
                    <br><br>
                    Mania sets consisting only of insane and above difficulties can omit hit sounds due to very few in the community at 
                    that level enjoying them in general. See <a href=""https://osu.ppy.sh/community/forums/topics/996091"">
                    [Proposal - mania] Guidelines allowing higher difficulties to omit hitsound additions.</a> Since we're missing mania 
                    SR support we'll simply exclude mania from this check entirely."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "No Hit Sounds",
                    new IssueTemplate(Issue.Level.Problem,
                        "This beatmap contains no hit sounds or sampleset changes.")
                    .WithCause(
                        "There are no hit sounds or sampleset changes anywhere in a difficulty.") },

                { "Problem",
                    new IssueTemplate(Issue.Level.Problem,
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

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            double prevTime = beatmap.hitObjects.Count > 0 ? beatmap.hitObjects.First().time : 0;
            int objectsPassed = 0;
            int totalHitSounds = 0;

            Beatmap.Sampleset? prevSample = null;

            List<Issue> issues = new List<Issue>();
            
            void ApplyFeedbackUpdate(HitObject.HitSound hitSound, Beatmap.Sampleset sampleset, HitObject hitObject, double time)
            {
                if (prevSample == null)
                    prevSample = sampleset;

                if (hitSound > 0 ||
                    sampleset != prevSample ||
                    beatmap.generalSettings.mode == Beatmap.Mode.Mania && (hitObject.filename ?? "") != "")
                {
                    prevSample = sampleset;

                    ++totalHitSounds;
                    Issue issue = GetIssueFromUpdate(time, ref objectsPassed, ref prevTime, beatmap);
                    if (issue != null)
                        issues.Add(issue);
                }
                else
                    ++objectsPassed;
            }

            foreach (HitObject hitObject in beatmap.hitObjects)
            {
                while (true)
                {
                    // Breaks and spinners don't really need to be hit sounded so we take that into account
                    // by looking for any between the current object and excluding their drain time if present.
                    Break @break =
                        beatmap.breaks.FirstOrDefault(otherBreak =>
                            otherBreak.endTime > prevTime &&
                            otherBreak.endTime < hitObject.time);
                    Spinner spinner =
                        beatmap.hitObjects.OfType<Spinner>().FirstOrDefault(otherSpinner =>
                            otherSpinner.endTime > prevTime &&
                            otherSpinner.endTime < hitObject.time);
                    
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

                    HitObject objectBeforeExcl = beatmap.GetHitObject(excludeStart - 1);
                    HitObject objectAfterExcl = beatmap.GetNextHitObject(excludeEnd);

                    double endTimeBeforeExcl =
                        objectBeforeExcl is Spinner ? ((Spinner)objectBeforeExcl).endTime
                        : objectBeforeExcl is Slider ? ((Slider)objectBeforeExcl).endTime
                        : objectBeforeExcl.time;

                    // Between the previous object's time and the end time before the exclusion,
                    // storyboarded hit sounds should be accounted for in mania, since they need to
                    // use them as substitutes to actual hit sounding.
                    foreach (Issue storyIssue in GetStoryHsIssuesFromUpdates(beatmap, prevTime, endTimeBeforeExcl, ref objectsPassed, ref prevTime))
                        issues.Add(storyIssue);
                    
                    // Exclusion happens through updating prevTime manually rather than through the update function.
                    Issue issue = GetIssueFromUpdate(endTimeBeforeExcl, ref objectsPassed, ref prevTime, beatmap);
                    if (issue != null)
                        issues.Add(issue);
                    prevTime = objectAfterExcl.time;
                }
                
                // Regardless of there being a spinner or break, storyboarded hit sounds should still be taken into account.
                foreach (Issue storyIssue in GetStoryHsIssuesFromUpdates(beatmap, prevTime, hitObject.time, ref objectsPassed, ref prevTime))
                    issues.Add(storyIssue);

                if (hitObject is Circle)
                    ApplyFeedbackUpdate(hitObject.hitSound, hitObject.GetSampleset(), hitObject, hitObject.time);

                if (hitObject is Slider slider)
                {
                    ApplyFeedbackUpdate(slider.startHitSound, slider.GetStartSampleset(), slider, slider.time);

                    if (slider.reverseHitSounds.Any())
                        for (int reverseIndex = 0; reverseIndex < slider.edgeAmount - 1; ++reverseIndex)
                            ApplyFeedbackUpdate(
                                slider.reverseHitSounds.ElementAt(reverseIndex),
                                slider.GetReverseSampleset(reverseIndex),
                                slider,
                                Math.Floor(slider.time + slider.GetCurveDuration() * (reverseIndex + 1)));

                    ApplyFeedbackUpdate(slider.endHitSound, slider.GetEndSampleset(), slider, slider.endTime);
                }
            }
            
            if (totalHitSounds == 0)
                yield return new Issue(GetTemplate("No Hit Sounds"), beatmap);
            else
                foreach (Issue issue in issues)
                    yield return issue;
        }

        /// <summary> Returns an issue when too much time and/or too many objects were passed before this method was called again. </summary>
        private Issue GetIssueFromUpdate(double currentTime, ref int objectsPassed, ref double previousTime, Beatmap beatmap)
        {
            double prevTime = previousTime;

            double timeDifference = currentTime - prevTime;
            double objectDifference = objectsPassed;
            
            objectsPassed = 0;
            previousTime = currentTime;

            double timeRatio = timeDifference;
            double objectRatio = objectDifference * 200;

            // Thresholds
            int warningTotal    = 10000;
            int warningTime     = 8 * 1500;     // 12 seconds (8 measures of 160 BPM, usually makes up a whole section in the song)
            int warningObject   = 2 * 200;      // 2 objects (difficulty invariant, so needs to work for easy diffs too)

            int problemTotal     = 30000;
            int problemTime      = 24 * 1500;    // 36 seconds (24 measures of 160 BPM, usually makes up multiple sections in the song)
            int problemObject    = 8 * 200;      // 8 objects
            
            if (timeRatio + objectRatio > problemTotal &&    // at least this much of the combined
                timeRatio > problemTime &&                   // at least this much of the individual
                objectRatio > problemObject)
            {
                return new Issue(GetTemplate("Problem"), beatmap,
                    Timestamp.Get(currentTime - timeDifference), Timestamp.Get(currentTime),
                    $"{timeDifference / 1000:0.##}");
            }

            else if (
                timeRatio + objectRatio > warningTotal &&
                timeRatio > warningTime &&
                objectRatio > warningObject)
            {
                return new Issue(GetTemplate("Warning"), beatmap,
                    Timestamp.Get(currentTime - timeDifference), Timestamp.Get(currentTime),
                    $"{timeDifference / 1000:0.##}");
            }
            
            return null;
        }

        /// <summary> Returns issues for every storyboarded hit sound where too much time and/or too many objects were passed since last update. </summary>
        private List<Issue> GetStoryHsIssuesFromUpdates(Beatmap beatmap, double startTime, double endTime, ref int objectsPassed, ref double prevTime)
        {
            List<Issue> issues = new List<Issue>();
            if (beatmap.generalSettings.mode == Beatmap.Mode.Mania)
            {
                while (true)
                {
                    Sample storyHitSound = beatmap.samples.FirstOrDefault(
                        aHitsound => aHitsound.time > startTime && aHitsound.time < endTime);

                    if (storyHitSound == null)
                        break;

                    Issue maniaIssue = GetIssueFromUpdate(storyHitSound.time, ref objectsPassed, ref prevTime, beatmap);
                    if (maniaIssue != null)
                        issues.Add(maniaIssue);

                    startTime = prevTime;
                }
            }
            return issues;
        }
    }
}
