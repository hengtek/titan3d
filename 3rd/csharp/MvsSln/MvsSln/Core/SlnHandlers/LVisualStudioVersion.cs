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
using net.r_eg.MvsSln.Log;

namespace net.r_eg.MvsSln.Core.SlnHandlers
{
    public class LVisualStudioVersion: LAbstract, ISlnHandler
    {
        protected LineType lineType;

        protected enum LineType
        {
            Unknown,
            FormatVersion,
            ProgramVersion,
            VisualStudioVersion,
            MinimumVisualStudioVersion,
        }

        /// <summary>
        /// Checks the readiness to process data.
        /// </summary>
        /// <param name="svc"></param>
        /// <returns>True value if it's ready at current time.</returns>
        public override bool IsActivated(ISvc svc)
        {
            return ((svc.Sln.ResultType & SlnItems.Header) == SlnItems.Header);
        }

        /// <summary>
        /// Condition for line to continue processing.
        /// </summary>
        /// <param name="line"></param>
        /// <returns>true value to continue.</returns>
        public override bool Condition(RawText line)
        {
            return Eq(line.trimmed, out lineType);
        }

        /// <summary>
        /// New position in stream.
        /// </summary>
        /// <param name="svc"></param>
        /// <param name="line">Received line.</param>
        /// <returns>true if it was processed by current handler, otherwise it means ignoring.</returns>
        public override bool Positioned(ISvc svc, RawText line)
        {
            int pos = line.trimmed.LastIndexOfAny(new char[] { '=', ' ' });
            string version;

            if(pos == -1 
                || lineType == LineType.Unknown 
                || String.IsNullOrWhiteSpace(version = line.trimmed.Substring(pos + 1)))
            {
                LSender.Send(this, $"Incorrect version info from header: '{line.trimmed}'", Message.Level.Warn);
                return false;
            }

            LSender.Send(this, $"Found version from header: '{lineType}' = '{version}'", Message.Level.Info);

            var h = new SlnHeader(svc.Sln.Header);
            switch(lineType)
            {
                case LineType.FormatVersion: {
                    h.SetFormatVersion(version);
                    break;
                }
                case LineType.VisualStudioVersion: {
                    h.SetVisualStudioVersion(version);
                    break;
                }
                case LineType.MinimumVisualStudioVersion: {
                    h.SetMinimumVersion(version);
                    break;
                }
                case LineType.ProgramVersion: {
                    h.SetProgramVersion(version);
                    break;
                }
                default: {
                    return false;
                }
            }

            svc.Sln.SetHeader(h);
            return true;
        }

        protected bool Eq(string line, out LineType type)
        {
            bool _cmp(string _a, string _b)
            {
                return _a.StartsWith(_b, StringComparison.Ordinal);
            };

            if(_cmp(line, "Microsoft Visual Studio Solution File, Format Version")) {
                type = LineType.FormatVersion;
                return true;
            }

            if(_cmp(line, "MinimumVisualStudioVersion")) {
                type = LineType.MinimumVisualStudioVersion;
                return true;
            }

            if(_cmp(line, "VisualStudioVersion")) {
                type = LineType.VisualStudioVersion;
                return true;
            }

            if(_cmp(line, "# Visual Studio")) {
                type = LineType.ProgramVersion;
                return true;
            }

            type = LineType.Unknown;
            return false;
        }
    }
}
