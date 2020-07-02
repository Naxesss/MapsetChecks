using MapsetParser.objects;
using MapsetParser.statics;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MapsetChecks
{
    public class Common
    {
        public const string CHECK_MANUALLY_MESSAGE = ", so you'll need to check that manually.";
        public const string FILE_EXCEPTION_MESSAGE = "\"{0}\" couldn't be checked, so you'll need to do that manually.{1}";

        public static string AsExceptionDiv(Exception exception)
        {
            return $@"
                <div
                    class=""exception-shortcut detail-shortcut shows-info""
                    data-tooltip=""Show exception info""
                    data-shown-info=""
                        <div class=&quot;exception-message&quot;>
                            &quot;{exception.Message.Replace("\"", "&quot;").Replace("\n", "<br />")}&quot;
                        </div>
                        <div class=&quot;paste-separator&quot;></div>
                        <div class=&quot;exception-trace&quot;>
                            {exception.StackTrace.Replace("\"", "&quot;").Replace("\n", "<br />")}
                        </div>"">
                </div>";
        }

        public static IEnumerable<Issue> GetInconsistencies(
            BeatmapSet beatmapSet,
            Func<Beatmap, string> ConsistencyCheck,
            IssueTemplate template)
        {
            List<KeyValuePair<Beatmap, string>> pairs = new List<KeyValuePair<Beatmap, string>>();
            foreach (Beatmap beatmap in beatmapSet.beatmaps)
                pairs.Add(new KeyValuePair<Beatmap, string>(beatmap, ConsistencyCheck(beatmap)));

            var groups = pairs.Where(pair => pair.Value != null).GroupBy(pair => pair.Value).Select(group =>
                new KeyValuePair<string, IEnumerable<Beatmap>>(group.Key, group.Select(pair => pair.Key)));

            if (groups.Count() > 1)
            {
                foreach (var group in groups)
                {
                    string message = group.Key + " : " + String.Join(" ", group.Value);
                    yield return new Issue(template, null, message);
                }
            }
        }

        public struct TagFile
        {
            public readonly TagLib.File file;
            public readonly string templateName;
            public readonly object[] templateArgs;

            public TagFile(TagLib.File file, string templateName, object[] templateArgs)
            {
                this.file = file;
                this.templateName = templateName;
                this.templateArgs = templateArgs;
            }
        }

        public static IEnumerable<Issue> GetTagOsuIssues(
            BeatmapSet beatmapSet,
            Func<Beatmap, IEnumerable<string>> BeatmapFunc,
            Func<string, IssueTemplate> TemplateFunc,
            Func<TagFile, List<Issue>> SuccessFunc)
        {
            IEnumerable<TagFile> tagFiles = GetTagOsuFiles(beatmapSet, BeatmapFunc);
            foreach (TagFile tagFile in tagFiles)
            {
                // error
                if (tagFile.file == null)
                    yield return new Issue(TemplateFunc(tagFile.templateName), null,
                        tagFile.templateArgs.ToArray());

                // success
                else
                    foreach (Issue issue in SuccessFunc(tagFile))
                        yield return issue;
            }
        }

        public static IEnumerable<Issue> GetTagOsbIssues(
            BeatmapSet beatmapSet,
            Func<Osb, IEnumerable<string>> OsbFunc,
            Func<string, IssueTemplate> TemplateFunc,
            Func<TagFile, List<Issue>> SuccessFunc)
        {
            IEnumerable<TagFile> tagFiles = GetTagOsbFiles(beatmapSet, OsbFunc);
            foreach (TagFile tagFile in tagFiles)
            {
                if (tagFile.file == null)
                {
                    List<object> templateArgs = new List<object> { tagFile.templateArgs[0] };
                    if (tagFile.templateArgs.Count() > 1)
                        templateArgs.Add(tagFile.templateArgs[1]);

                    yield return new Issue(TemplateFunc(tagFile.templateName), null,
                        templateArgs.ToArray());
                }

                else
                    foreach (Issue issue in SuccessFunc(tagFile))
                        yield return issue;
            }
        }

        public static IEnumerable<TagFile> GetTagOsuFiles(BeatmapSet beatmapSet, Func<Beatmap, IEnumerable<string>> BeatmapFunc)
        {
            List<string> fileNames = new List<string>();
            foreach (Beatmap beatmap in beatmapSet.beatmaps)
            {
                IEnumerable<string> fileNameList = BeatmapFunc(beatmap);

                if (fileNameList != null)
                    foreach (string fileName in fileNameList)
                        if (fileName != null && !fileNames.Contains(fileName))
                            fileNames.Add(fileName);
            }

            return GetTagFiles(beatmapSet, fileNames);
        }

        public static IEnumerable<TagFile> GetTagOsbFiles(BeatmapSet beatmapSet, Func<Osb, IEnumerable<string>> OsbFunc)
        {
            List<string> fileNames = new List<string>();
            if (beatmapSet.osb != null)
            {
                IEnumerable<string> fileNameList = OsbFunc(beatmapSet.osb);

                if (fileNameList != null)
                    foreach (string fileName in fileNameList)
                        if (fileName != null && !fileNames.Contains(fileName))
                            fileNames.Add(fileName);
            }

            return GetTagFiles(beatmapSet, fileNames);
        }

        public static IEnumerable<TagFile> GetTagFiles(BeatmapSet beatmapSet, List<string> fileNames)
        {
            if (beatmapSet.songPath != null)
            {
                foreach (string fileName in fileNames)
                {
                    TagLib.File file = null;
                    string errorTemplate = "";
                    List<object> arguments = new List<object>() { fileName };

                    if (fileName.StartsWith(".."))
                    {
                        errorTemplate = "Leaves Folder";
                    }
                    else
                    {
                        string[] files;
                        try
                        { files = Directory.GetFiles(beatmapSet.songPath, fileName + (fileName.Contains(".") ? "" : ".*")); }
                        catch (DirectoryNotFoundException)
                        { files = new string[] { }; }

                        if (files.Length > 0)
                        {
                            try
                            { file = new FileAbstraction(files[0]).GetTagFile(); }
                            catch (Exception exception)
                            {
                                errorTemplate = "Exception";
                                arguments.Add(AsExceptionDiv(exception));
                            }
                        }
                        else
                            errorTemplate = "Missing";
                    }

                    yield return new TagFile(file, errorTemplate, arguments.ToArray());
                }
            }
        }

        /// <summary> Collects the usage data of the given hit sound, as well as the timestamp where it's
        /// been most frequently used. </summary>
        public static void CollectHitSoundFrequency(BeatmapSet aBeatmapSet, string aHsFileName, double scoreThreshold,
            out string aMostFrequentTimestamp, out Dictionary<Beatmap, int> aUseData)
        {
            aUseData = new Dictionary<Beatmap, int>();
            double prevTime = 0;
            double highestFrequencyScore = 0;
            double frequencyScore = 0;
            aMostFrequentTimestamp = null;
            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
            {
                aUseData[beatmap] = 0;
                frequencyScore = 0;
                prevTime = beatmap.hitObjects.FirstOrDefault()?.time ?? 0;
                foreach (HitObject hitObject in beatmap.hitObjects)
                {
                    if (!hitObject.usedHitSamples.Any(aSample => aSample.SameFileName(aHsFileName)))
                        continue;

                    ++aUseData[beatmap];
                    frequencyScore += beatmap.generalSettings.mode == Beatmap.Mode.Mania ? 0.5 : 1;

                    double deltaTime = hitObject.time - prevTime;
                    double mult = Math.Pow(0.8, 1 / 1000d * deltaTime);
                    frequencyScore *= mult;
                    prevTime = hitObject.time;

                    if (highestFrequencyScore < frequencyScore)
                    {
                        if (frequencyScore >= scoreThreshold)
                            aMostFrequentTimestamp = $"{Timestamp.Get(hitObject)} in {beatmap}";

                        highestFrequencyScore = frequencyScore;
                    }
                }
            }
        }

        /// <summary> Returns the beatmap in the given beatmapset which most commonly uses some hit sound. </summary>
        public static Beatmap GetBeatmapCommonlyUsedIn(BeatmapSet aBeatmapSet, Dictionary<Beatmap, int> aUseData,
            double commonUsageThreshold)
        {
            double mostUses = 0;
            Beatmap mapMostCommonlyUsedIn = null;
            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
            {
                if (IsHitSoundCommonlyUsed(beatmap, aUseData[beatmap], commonUsageThreshold) &&
                    mostUses < aUseData[beatmap])
                {
                    mapMostCommonlyUsedIn = beatmap;
                    mostUses = aUseData[beatmap];
                }
            }

            return mapMostCommonlyUsedIn;
        }

        /// <summary> Returns whether the drain time to use ratio exceeds the common usage threshold. </summary>
        private static bool IsHitSoundCommonlyUsed(Beatmap aBeatmap, double aUses, double aCommonUsageThreshold)
        {
            if (aUses == 0)
                return false;

            // Mania can have multiple objects per moment in time, so we arbitrarily divide its usage by 2.
            if (aBeatmap.generalSettings.mode == Beatmap.Mode.Mania)
                aUses /= 2f;

            double mean = aBeatmap.GetDrainTime() / aUses;
            return mean <= aCommonUsageThreshold;
        }
    }
}
