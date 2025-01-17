﻿/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2013-2021  Denis Kuzmin <x-3F@outlook.com> github/3F
 * Copyright (c) MvsSln contributors https://github.com/3F/MvsSln/graphs/contributors
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using net.r_eg.MvsSln.Core;

namespace net.r_eg.MvsSln.Projects
{
    internal static class PackagesConfigLocator
    {
        internal const string DIR = "packages";

        public static IEnumerable<PackagesConfig> FindAndLoadConfigs(ISlnResult sln, SlnItems items)
            => FindConfigs(sln, items)
                .Select(c => new PackagesConfig(c, PackagesConfigOptions.Load 
                                                    | PackagesConfigOptions.PathToStorage 
                                                    | PackagesConfigOptions.SilentLoading));

        public static IEnumerable<string> FindConfigs(ISlnResult sln, SlnItems items)
            => FindAllConfigs(sln, items).Distinct();

        private static IEnumerable<string> FindAllConfigs(ISlnResult sln, SlnItems items)
        {
            if(sln == null) throw new ArgumentNullException(nameof(sln));

            if(items.HasFlag(SlnItems.PackagesConfigSolution))
            {
                foreach(var config in FindSolutionConfigs(sln, items)) yield return config;
            }

            if(items.HasFlag(SlnItems.PackagesConfigLegacy))
            {
                foreach(var config in FindLegacyConfigs(sln)) yield return config;
            }
        }

        private static IEnumerable<string> FindSolutionConfigs(ISlnResult sln, SlnItems items)
        {
            string dfile = Path.GetFullPath(Path.Combine(sln.SolutionDir, PackagesConfig.FNAME));
            if(File.Exists(dfile)) yield return dfile;

            if(sln.SolutionFolders != null)
            foreach(RawText file in sln.SolutionFolders.SelectMany(f => f.items))
            {
                if(!file.trimmed.EndsWith(PackagesConfig.FNAME)) continue;

                string input = Path.GetFullPath(Path.Combine(sln.SolutionDir, file));
                if(File.Exists(input)) yield return input;
            }
        }

        private static IEnumerable<string> FindLegacyConfigs(ISlnResult sln)
        {
            string dfile = Path.GetFullPath(Path.Combine(sln.SolutionDir, DIR, PackagesConfig.FNAME));
            if(File.Exists(dfile)) yield return dfile;

            if(sln.ProjectItems != null)
            foreach(var prj in sln.ProjectItems)
            {
                string input = Path.Combine(Path.GetDirectoryName(prj.fullPath), PackagesConfig.FNAME);
                if(File.Exists(input)) yield return input;
            }
        }
    }
}
