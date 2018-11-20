using System;
using System.Reflection;

namespace AssetGenerator
{
    public class Property
    {
        public PropertyName Name;
        public string ReadmeColumnName;
        public string ReadmeValue;
        public Func<object> Value { get; set; }

        public Property(PropertyName enumName, object displayValue)
        {
            Name = enumName;
            ReadmeColumnName = ReadmeStringHelper.GenerateNameWithSpaces(enumName.ToString());
            ReadmeValue = ReadmeStringHelper.ConvertValueToString(displayValue);
        }

        public override bool Equals(object obj)
        {
            Property otherProperty = obj as Property;
            if (Name == otherProperty.Name)
            {
                return ReadmeValue == otherProperty.ReadmeValue;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }

    /// <summary>
    /// Pass an object to CloneObject, and it returns a deep copy of that object.
    /// </summary>
    public static class DeepCopy
    {
        public static T CloneObject<T>(T obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("Object cannot be null");
            }
            return (T)Process(obj);
        }
        static object Process(object obj)
        {
            if (obj == null)
            {
                return null;
            }
            Type type = obj.GetType();
            if (type.IsValueType || type == typeof(string))
            {
                return obj;
            }
            else if (type.IsArray)
            {
                Type elementType = Type.GetType(
                    type.FullName.Replace("[]", string.Empty));
                if (elementType == null) // Catch for types in System.Numerics
                {
                    elementType = Type.GetType(
                        type.AssemblyQualifiedName.ToString().Replace("[]", string.Empty));
                }
                var array = obj as Array;
                Array copied = Array.CreateInstance(elementType, array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    copied.SetValue(Process(array.GetValue(i)), i);
                }
                return Convert.ChangeType(copied, obj.GetType());
            }
            else if (type.IsClass)
            {
                object toret = Activator.CreateInstance(obj.GetType());
                FieldInfo[] fields = type.GetFields(BindingFlags.Public |
                            BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    object fieldValue = field.GetValue(obj);
                    if (fieldValue == null)
                        continue;
                    field.SetValue(toret, Process(fieldValue));
                }
                return toret;
            }
            else
            {
                throw new ArgumentException("Unknown type");
            }
        }
    }

    public enum PropertyName
    {
        Mode,
        IndicesValues,
        IndicesComponentType,
        AlphaMode,
        AlphaCutoff,
        DoubleSided,
        VertexUV0,
        VertexNormal,
        VertexTangent,
        VertexColor,
        NormalTexture,
        Normals,
        NormalTextureScale,
        OcclusionTexture,
        OcclusionTextureStrength,
        EmissiveTexture,
        EmissiveFactor,
        ExtensionUsed,
        SpecularGlossinessOnMaterial0,
        SpecularGlossinessOnMaterial1,
        BaseColorTexture,
        BaseColorFactor,
        MetallicRoughnessTexture,
        MetallicFactor,
        RoughnessFactor,
        DiffuseTexture,
        DiffuseFactor,
        SpecularGlossinessTexture,
        SpecularFactor,
        GlossinessFactor,
        Primitive0,
        Primitive1,
        Material0WithBaseColorFactor,
        Material1WithBaseColorFactor,
        Primitive0VertexUV0,
        Primitive1VertexUV0,
        Primitive0VertexUV1,
        Primitive1VertexUV1,
        Matrix,
        Translation,
        Rotation,
        Scale,
        Target,
        Interpolation,
        WrapT,
        WrapS,
        MagFilter,
        MinFilter,
        Version,
        MinVersion,
        Description,
        ModelShouldLoad,
    }
}
