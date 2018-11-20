using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
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
