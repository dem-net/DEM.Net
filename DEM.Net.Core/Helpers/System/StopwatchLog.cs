// StopwatchLog.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2020 Xavier Fischer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the right
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
    public class StopwatchLog : Stopwatch
    {
        private readonly ILogger _logger;
        private TimeSpan _lastTime = TimeSpan.Zero;
        public static StopwatchLog StartNew(ILogger logger)
        {
            StopwatchLog sw = new StopwatchLog(logger);
            sw.Start();
            return sw;
        }
        public StopwatchLog(ILogger logger)
        {
            this._logger = logger;            
        }

        public void LogTime(string operationName = "Operation", LogLevel level = LogLevel.Information)
        {
            this.Stop();
            if (_logger.IsEnabled(level))
            {
                _logger.Log(level, $"{operationName} completed in {(this.Elapsed - _lastTime).TotalMilliseconds:N1} ms (Total: {this.ElapsedMilliseconds:N1} ms)");
            }
            _lastTime = this.Elapsed;
            this.Start();
        }
        
    }

}
