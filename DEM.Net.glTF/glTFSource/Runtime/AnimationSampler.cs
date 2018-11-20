using System.Collections.Generic;

namespace AssetGenerator.Runtime
{
    public abstract class AnimationSampler
    {
        public abstract IEnumerable<float> InputKeys { get; }
    }

    public class StepAnimationSampler<T> : AnimationSampler
    {
        public override IEnumerable<float> InputKeys { get; }

        public IEnumerable<T> OutputKeys { get; }

        public StepAnimationSampler(IEnumerable<float> inputKeys, IEnumerable<T> outputKeys)
        {
            InputKeys = inputKeys;
            OutputKeys = outputKeys;
        }
    }

    public class LinearAnimationSampler<T> : AnimationSampler
    {
        public override IEnumerable<float> InputKeys { get; }

        public IEnumerable<T> OutputKeys { get; }

        public LinearAnimationSampler(IEnumerable<float> inputKeys, IEnumerable<T> outputKeys)
        {
            InputKeys = inputKeys;
            OutputKeys = outputKeys;
        }
    }

    public class CubicSplineAnimationSampler<T> : AnimationSampler
    {
        public struct Key
        {
            public T InTangent;
            public T Value;
            public T OutTangent;

            public Key(T inTangent, T value, T outTangent)
            {
                this.InTangent = inTangent;
                this.Value = value;
                this.OutTangent = outTangent;
            }
        }

        public override IEnumerable<float> InputKeys { get; }

        public IEnumerable<Key> OutputKeys { get; }

        public CubicSplineAnimationSampler(IEnumerable<float> inputKeys, IEnumerable<Key> outputKeys)
        {
            InputKeys = inputKeys;
            OutputKeys = outputKeys;
        }
    }
}
