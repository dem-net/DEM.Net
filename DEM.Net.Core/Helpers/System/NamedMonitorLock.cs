using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core.Helpers
{
    public class NamedMonitor
    {
        readonly ConcurrentDictionary<string, object> _dictionary = new ConcurrentDictionary<string, object>();

        public object this[string name] => _dictionary.GetOrAdd(name, _ => new object());
    }
}
