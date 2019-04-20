#pragma warning disable 1591
namespace AssetGenerator.Runtime
{
    public class AnimationChannelTarget
    {
        public Node Node { get; set; }

        public enum PathEnum { TRANSLATION, ROTATION, SCALE, WEIGHT }

        public PathEnum Path { get; set; }
    }
}

#pragma warning restore 1591