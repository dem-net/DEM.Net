using System;
using System.Collections.Generic;

namespace AssetGenerator.Runtime
{
    public class Animation
    {
        public String Name { get; set; }
        public IEnumerable<AnimationChannel> Channels { get; set; }
    }
}
