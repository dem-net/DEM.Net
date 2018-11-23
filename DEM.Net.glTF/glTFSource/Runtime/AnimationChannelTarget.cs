namespace AssetGenerator.Runtime
{
    public class AnimationChannelTarget
    {
        public Node Node { get; set; }

        public enum PathEnum { TRANSLATION, ROTATION, SCALE, WEIGHT }

        public PathEnum Path { get; set; }
    }
}
