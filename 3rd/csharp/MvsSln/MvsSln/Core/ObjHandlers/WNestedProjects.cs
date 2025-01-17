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
using System.Text;
using net.r_eg.MvsSln.Extensions;

namespace net.r_eg.MvsSln.Core.ObjHandlers
{
    public class WNestedProjects: WAbstract, IObjHandler
    {
        protected IEnumerable<SolutionFolder> folders;

        protected IEnumerable<ProjectItem> pItems;

        /// <summary>
        /// To extract prepared raw-data.
        /// </summary>
        /// <param name="data">Any object data which is ready for this IObjHandler.</param>
        /// <returns>Final part of sln data.</returns>
        public override string Extract(object data)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"{SP}GlobalSection(NestedProjects) = preSolution");
            bool hasDep = false;

            folders?.ForEach(p =>
            {
                if(p.header.parent?.Value != null)
                {
                    sb.AppendLine($"{SP}{SP}{p.header.pGuid} = {p.header.parent.Value?.header.pGuid}");
                    hasDep = true;
                }
            });

            pItems?.ForEach(p =>
            {
                if(p.parent?.Value != null)
                {
                    sb.AppendLine($"{SP}{SP}{p.pGuid} = {p.parent.Value?.header.pGuid}");
                    hasDep = true;
                }
            });

            if(!hasDep) {
                return String.Empty;
            }

            sb.Append($"{SP}EndGlobalSection");
            return sb.ToString();
        }

        /// <param name="folders">Information about folders.</param>
        public WNestedProjects(IEnumerable<SolutionFolder> folders)
            : this(folders, null)
        {

        }

        /// <param name="pItems">Information about project items.</param>
        public WNestedProjects(IEnumerable<ProjectItem> pItems)
            : this(null, pItems)
        {

        }

        /// <param name="folders">Information about folders.</param>
        /// <param name="pItems">Information about project items.</param>
        public WNestedProjects(IEnumerable<SolutionFolder> folders, IEnumerable<ProjectItem> pItems)
        {
            this.folders    = folders;
            this.pItems     = pItems;
        }
    }
}
