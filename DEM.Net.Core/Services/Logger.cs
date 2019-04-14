// Logger.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2019 
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
	public static class Logger
	{
		public static void Info(string message)
		{
			Trace.TraceInformation(message);
		}
		public static void Info(string message, params object[] args)
		{
			Trace.TraceInformation(message, args);
		}
		public static void Warning(string message)
		{
			Trace.TraceWarning(message);
		}
		public static void Warning(string message, params object[] args)
		{
			Trace.TraceWarning(message, args);
		}
		public static void Error(string message)
		{
			Trace.TraceError(message);
		}
		public static void Error(string message, params object[] args)
		{
			Trace.TraceError(message, args);
		}


		private static Dictionary<string, Stopwatch> _stopWatches = new Dictionary<string, Stopwatch>();
		public static void StartPerf(string key)
		{
			if (_stopWatches.ContainsKey(key))
			{
				_stopWatches[key].Start();
			}
			else
			{
				_stopWatches[key] = Stopwatch.StartNew();
			}
		}
		public static void RestartPerf(string key)
		{
			_stopWatches[key] = Stopwatch.StartNew();
		}
		public static TimeSpan StopPerf(string key, bool log = true)
		{
			Stopwatch sw = _stopWatches[key];
			sw.Stop();
			if (log)
			{
				Logger.Info($"Perf {key} : {sw.Elapsed:g}");
			}

			return sw.Elapsed;
		}
	}
}
