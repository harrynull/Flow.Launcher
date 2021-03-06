﻿using Flow.Launcher.Infrastructure.Logger;
using Flow.Launcher.Plugin.SharedCommands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Flow.Launcher.Plugin.Explorer.Search.DirectoryInfo
{
    public class DirectoryInfoSearch
    {
        private readonly ResultManager resultManager;

        public DirectoryInfoSearch(PluginInitContext context)
        {
            resultManager = new ResultManager(context);
        }

        internal List<Result> TopLevelDirectorySearch(Query query, string search, CancellationToken token)
        {
            var criteria = ConstructSearchCriteria(search);

            if (search.LastIndexOf(Constants.AllFilesFolderSearchWildcard) >
                search.LastIndexOf(Constants.DirectorySeperator))
                return DirectorySearch(new EnumerationOptions
                {
                    RecurseSubdirectories = true
                }, query, search, criteria, token);

            return DirectorySearch(new EnumerationOptions(), query, search, criteria, token); // null will be passed as default
        }

        public string ConstructSearchCriteria(string search)
        {
            string incompleteName = "";

            if (!search.EndsWith(Constants.DirectorySeperator))
            {
                var indexOfSeparator = search.LastIndexOf(Constants.DirectorySeperator);

                incompleteName = search.Substring(indexOfSeparator + 1).ToLower();

                if (incompleteName.StartsWith(Constants.AllFilesFolderSearchWildcard))
                    incompleteName = "*" + incompleteName.Substring(1);
            }

            incompleteName += "*";

            return incompleteName;
        }

        private List<Result> DirectorySearch(EnumerationOptions enumerationOption, Query query, string search,
            string searchCriteria, CancellationToken token)
        {
            var results = new List<Result>();

            var path = FilesFolders.ReturnPreviousDirectoryIfIncompleteString(search);

            var folderList = new List<Result>();
            var fileList = new List<Result>();

            try
            {
                var directoryInfo = new System.IO.DirectoryInfo(path);

                foreach (var fileSystemInfo in directoryInfo.EnumerateFileSystemInfos(searchCriteria, enumerationOption))
                {
                    if (fileSystemInfo is System.IO.DirectoryInfo)
                    {
                        folderList.Add(resultManager.CreateFolderResult(fileSystemInfo.Name, fileSystemInfo.FullName,
                            fileSystemInfo.FullName, query, true, false));
                    }
                    else
                    {
                        fileList.Add(resultManager.CreateFileResult(fileSystemInfo.FullName, query, true, false));
                    }

                    token.ThrowIfCancellationRequested();
                }
            }
            catch (Exception e)
            {
                if (!(e is ArgumentException))
                    throw e;
                
                results.Add(new Result {Title = e.Message, Score = 501});

                return results;

#if DEBUG // Please investigate and handle error from DirectoryInfo search
#else
                Log.Exception($"|Flow.Launcher.Plugin.Explorer.DirectoryInfoSearch|Error from performing DirectoryInfoSearch", e);
#endif
            }

            // Initial ordering, this order can be updated later by UpdateResultView.MainViewModel based on history of user selection.
            return results.Concat(folderList.OrderBy(x => x.Title)).Concat(fileList.OrderBy(x => x.Title)).ToList();
        }
    }
}