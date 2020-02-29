//
// TimeSpanBlock.cs
//
// Author:
//       Xavier Fischer 2019-9
//
// Copyright (c) 2019 Xavier Fischer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DEM.Net.Core
{
    public class TimeSpanBlock : IDisposable
    {
        private StopwatchLog _sw;
        private readonly string _operationName;
        private readonly LogLevel _logLevel;

        public TimeSpanBlock(string operationName, ILogger logger, LogLevel logLevel = LogLevel.Information)
        {
            _sw = StopwatchLog.StartNew(logger);
            _operationName = operationName;
            _logLevel = logLevel;
        }

        public TimeSpan Elapsed => _sw.Elapsed;

        public void Dispose()
        {
            _sw.LogTime(_operationName, _logLevel);
        }
    }
}
