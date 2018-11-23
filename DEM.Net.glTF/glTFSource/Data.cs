using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace AssetGenerator
{
    public class Data : IDisposable
    {
        public string Name { get; private set; }
        public BinaryWriter Writer { get; private set; }

        public Data(string name)
        {
            this.Name = name;
            this.Writer = new BinaryWriter(new MemoryStream());
        }

        public void Dispose()
        {
            this.Writer.BaseStream.Dispose();
            this.Writer.Dispose();
        }

        public byte[] ToArray()
        {
            this.Writer.Flush();
            return ((MemoryStream)this.Writer.BaseStream).ToArray();
        }
    }

    public static class BinaryWriterExtensions
    {
        public static void Write(this BinaryWriter writer, Quaternion value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
            writer.Write(value.W);
        }

        public static void Write(this BinaryWriter writer, IEnumerable<Quaternion> values)
        {
            values.ForEach(value => writer.Write(value));
        }

        public static void Write(this BinaryWriter writer, Vector4 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
            writer.Write(value.W);
        }

        public static void Write(this BinaryWriter writer, IEnumerable<Vector4> values)
        {
            values.ForEach(value => writer.Write(value));
        }

        public static void Write(this BinaryWriter writer, Vector3 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }

        public static void Write(this BinaryWriter writer, IEnumerable<Vector3> values)
        {
            values.ForEach(value => writer.Write(value));
        }

        public static void Write(this BinaryWriter writer, Vector2 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
        }

        public static void Write(this BinaryWriter writer, Matrix4x4 value)
        {
            writer.Write(value.M11);
            writer.Write(value.M12);
            writer.Write(value.M13);
            writer.Write(value.M14);
            writer.Write(value.M21);
            writer.Write(value.M22);
            writer.Write(value.M23);
            writer.Write(value.M24);
            writer.Write(value.M31);
            writer.Write(value.M32);
            writer.Write(value.M33);
            writer.Write(value.M34);
            writer.Write(value.M41);
            writer.Write(value.M42);
            writer.Write(value.M43);
            writer.Write(value.M44);
        }

        public static void Write(this BinaryWriter writer, IEnumerable<Vector2> values)
        {
            values.ForEach(value => writer.Write(value));
        }

        public static void Write(this BinaryWriter writer, IEnumerable<Single> values)
        {
            values.ForEach(value => writer.Write(value));
        }

        public static void Write(this BinaryWriter writer, IEnumerable<Matrix4x4> values)
        {
            values.ForEach(value => writer.Write(value));
        }
        
    }

    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> values, Action<T> action)
        {
            foreach (var value in values)
            {
                action(value);
            }
        }
    }
}
