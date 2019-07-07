using MapsetChecks.objects;
using MapsetParser.objects;
using MapsetVerifierFramework.objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MapsetChecks
{
    public class Common
    {
        public static IEnumerable<Issue> GetInconsistencies(
            BeatmapSet aBeatmapSet,
            Func<Beatmap, string> aConsistencyCheck,
            IssueTemplate aTemplate)
        {
            List<KeyValuePair<Beatmap, string>> pairs = new List<KeyValuePair<Beatmap, string>>();
            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
                pairs.Add(new KeyValuePair<Beatmap, string>(beatmap, aConsistencyCheck(beatmap)));

            var groups = pairs.Where(aPair => aPair.Value != null).GroupBy(aPair => aPair.Value).Select(aGroup =>
                new KeyValuePair<string, IEnumerable<Beatmap>>(aGroup.Key, aGroup.Select(aPair => aPair.Key)));

            if (groups.Count() > 1)
            {
                foreach (var group in groups)
                {
                    string message = group.Key + " : " + String.Join(" ", group.Value);
                    yield return new Issue(aTemplate, null, message);
                }
            }
        }

        public struct TagFile
        {
            public readonly TagLib.File file;
            public readonly string templateName;
            public readonly object[] templateArgs;

            public TagFile(TagLib.File aFile, string aTemplateName, object[] aTemplateArgs)
            {
                file = aFile;
                templateName = aTemplateName;
                templateArgs = aTemplateArgs;
            }
        }

        public static IEnumerable<Issue> GetTagOsuIssues(
            BeatmapSet aBeatmapSet,
            Func<Beatmap, IEnumerable<string>> aBeatmapFunc,
            Func<string, IssueTemplate> aTemplateFunc,
            Func<TagFile, List<Issue>> aSuccessFunc)
        {
            IEnumerable<TagFile> tagFiles = GetTagOsuFiles(aBeatmapSet, aBeatmapFunc);
            foreach (TagFile tagFile in tagFiles)
            {
                // error
                if (tagFile.file == null)
                {
                    List<object> templateArgs = new List<object> { tagFile.templateArgs[0] };
                    if (tagFile.templateArgs.Count() > 1)
                        templateArgs.Add(tagFile.templateArgs[1]);

                    yield return new Issue(aTemplateFunc(tagFile.templateName), null,
                        templateArgs.ToArray());
                }

                // success
                else
                    foreach (Issue issue in aSuccessFunc(tagFile))
                        yield return issue;
            }
        }

        public static IEnumerable<Issue> GetTagOsbIssues(
            BeatmapSet aBeatmapSet,
            Func<Osb, IEnumerable<string>> anOsbFunc,
            Func<string, IssueTemplate> aTemplateFunc,
            Func<TagFile, List<Issue>> aSuccessFunc)
        {
            IEnumerable<TagFile> tagFiles = GetTagOsbFiles(aBeatmapSet, anOsbFunc);
            foreach (TagFile tagFile in tagFiles)
            {
                if (tagFile.file == null)
                {
                    List<object> templateArgs = new List<object> { tagFile.templateArgs[0] };
                    if (tagFile.templateArgs.Count() > 1)
                        templateArgs.Add(tagFile.templateArgs[1]);

                    yield return new Issue(aTemplateFunc(tagFile.templateName), null,
                        templateArgs.ToArray());
                }

                else
                    foreach (Issue issue in aSuccessFunc(tagFile))
                        yield return issue;
            }
        }

        public static IEnumerable<TagFile> GetTagOsuFiles(BeatmapSet aBeatmapSet, Func<Beatmap, IEnumerable<string>> aBeatmapFunc)
        {
            List<string> fileNames = new List<string>();
            foreach (Beatmap beatmap in aBeatmapSet.beatmaps)
            {
                IEnumerable<string> fileNameList = aBeatmapFunc(beatmap);

                if (fileNameList != null)
                    foreach (string fileName in fileNameList)
                        if (fileName != null && !fileNames.Contains(fileName))
                            fileNames.Add(fileName);
            }

            return GetTagFiles(aBeatmapSet, fileNames);
        }

        public static IEnumerable<TagFile> GetTagOsbFiles(BeatmapSet aBeatmapSet, Func<Osb, IEnumerable<string>> anOsbFunc)
        {
            List<string> fileNames = new List<string>();
            if (aBeatmapSet.osb != null)
            {
                IEnumerable<string> fileNameList = anOsbFunc(aBeatmapSet.osb);

                if (fileNameList != null)
                    foreach (string fileName in fileNameList)
                        if (fileName != null && !fileNames.Contains(fileName))
                            fileNames.Add(fileName);
            }

            return GetTagFiles(aBeatmapSet, fileNames);
        }

        public static IEnumerable<TagFile> GetTagFiles(BeatmapSet aBeatmapSet, List<string> aFileNames)
        {
            if (aBeatmapSet.songPath != null)
            {
                foreach (string fileName in aFileNames)
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
                        string[] files = null;
                        try
                        { files = Directory.GetFiles(aBeatmapSet.songPath, fileName + (fileName.Contains(".") ? "" : ".*")); }
                        catch (DirectoryNotFoundException)
                        { files = new string[] { }; }

                        if (files.Length > 0)
                        {
                            try
                            { file = new FileAbstraction(files[0]).GetTagFile(); }
                            catch (Exception exception)
                            {
                                errorTemplate = "Exception";
                                arguments.Add(exception.Message);
                            }
                        }
                        else
                            errorTemplate = "Missing";
                    }

                    yield return new TagFile(file, errorTemplate, arguments.ToArray());
                }
            }
        }
    }
}
