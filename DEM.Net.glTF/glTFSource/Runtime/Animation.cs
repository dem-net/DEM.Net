using System;
using System.Collections.Generic;
#pragma warning disable 1591

namespace AssetGenerator.Runtime
{
    public class Animation
    {
        public String Name { get; set; }
        public IEnumerable<AnimationChannel> Channels { get; set; }
    }
}

#pragma warning restore 1591