using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace AssetGenerator
{
    public static class ReadmeStringHelper
    {
        public static string ConvertValueToString(dynamic value)
        {
            string output = "ERROR";
            if (value == null)
            {
                output = ":white_check_mark:";
            }
            else
            {
                Type valueType = value.GetType();

                if (valueType.Equals(typeof(Vector2)) ||
                    valueType.Equals(typeof(Vector3)) ||
                    valueType.Equals(typeof(Vector4)))
                {
                    output = value.ToString("N1").Replace('<', '[').Replace('>', ']').Replace(" ", "&nbsp;");
                }
                else if (valueType.Equals(typeof(List<int>)))
                {
                    var floatArray = value.ToArray();
                    string[] stringArray = new string[floatArray.Length];
                    for (int i = 0; i < floatArray.Length; i++)
                    {
                        stringArray[i] = floatArray[i].ToString();
                    }
                    output = String.Join(", ", stringArray);
                    output = $"[{output}]";
                }
                else if (valueType.Equals(typeof(List<Vector2>)) ||
                         valueType.Equals(typeof(List<Vector3>)) ||
                         valueType.Equals(typeof(List<Vector4>)))
                {
                    output = ":white_check_mark:";
                }
                else if (valueType.Equals(typeof(Runtime.Image)))
                {
                    // 18 is normal cell height. Use height=\"72\" width=\"72\" to clamp the size, but currently removed
                    // due to streching when the table is too wide. Using thumbnails of the intended size for now.
                    Regex changePath = new Regex(@"(.*)(?=\/)");
                    string thumbnailPath = changePath.Replace(value.Uri, "Figures/Thumbnails", 1);
                    output = $"[<img src=\"{thumbnailPath}\" align=\"middle\">]({value.Uri})";
                }
                else if (valueType.Equals(typeof(Matrix4x4)))
                {
                    List<List<float>> matrixFloat = new List<List<float>>();
                    List<List<string>> matrixString = new List<List<string>>();
                    matrixFloat.Add(new List<float>(){ value.M11, value.M12, value.M13, value.M14 });
                    matrixFloat.Add(new List<float>(){ value.M21, value.M22, value.M23, value.M24 });
                    matrixFloat.Add(new List<float>(){ value.M31, value.M32, value.M33, value.M34 });
                    matrixFloat.Add(new List<float>(){ value.M41, value.M42, value.M43, value.M44 });

                    foreach (var row in matrixFloat)
                    {
                        matrixString.Add(new List<string>());
                        foreach (var num in row)
                        {
                            matrixString.Last().Add(num.ToString("N1"));
                        }
                    }

                    output = "";
                    foreach (var row in matrixString)
                    {
                        output += $"[{String.Join(",&nbsp;", row)}]<br>";
                    }
                }
                else if (valueType.Equals(typeof(Quaternion)))
                {
                    output = String.Format("[{0:N1}, {1:N1}, {2:N1}, {3:N1}]", 
                        value.X, value.Y, value.Z, value.W).Replace(" ", "&nbsp;");
                }
                else if (valueType.Equals(typeof(Runtime.MeshPrimitive.TextureCoordsComponentTypeEnum)))
                {
                    if (value == Runtime.MeshPrimitive.TextureCoordsComponentTypeEnum.NORMALIZED_UBYTE)
                    {
                        output = "Byte";
                    }
                    else if (value == Runtime.MeshPrimitive.TextureCoordsComponentTypeEnum.NORMALIZED_USHORT)
                    {
                        output = "Short";
                    }
                    else
                    {
                        output = "Float";
                    }
                }
                else if (valueType.Equals(typeof(Runtime.MeshPrimitive.IndexComponentTypeEnum)))
                {
                    if (value == Runtime.MeshPrimitive.IndexComponentTypeEnum.UNSIGNED_BYTE)
                    {
                        output = "Byte";
                    }
                    else if (value == Runtime.MeshPrimitive.IndexComponentTypeEnum.UNSIGNED_SHORT)
                    {
                        output = "Short";
                    }
                    else
                    {
                        output = "Int";
                    }
                }
                else if (valueType.Equals(typeof(Runtime.MeshPrimitive.IndexComponentTypeEnum)))
                {
                    if (value == Runtime.MeshPrimitive.IndexComponentTypeEnum.UNSIGNED_BYTE)
                    {
                        output = "Byte";
                    }
                    else if (value == Runtime.MeshPrimitive.IndexComponentTypeEnum.UNSIGNED_SHORT)
                    {
                        output = "Short";
                    }
                    else
                    {
                        output = "Int";
                    }
                }
                else // Likely a type that is easy to convert
                {
                    if (valueType.Equals(typeof(float)))
                    {
                        output = value.ToString("0.0"); // Displays two digits for floats
                    }
                    else if (valueType.BaseType.Equals(typeof(Enum)))
                    {
                        output = GenerateNameWithSpaces(value.ToString(), fullName: true);
                    }
                    else
                    {
                        output = value.ToString();
                    }
                }

                if (output != "ERROR")
                {
                    return output;
                }
                else
                {
                    Console.WriteLine("Unable to convert the value for an attribute into a format that can be added to the log.");
                    return output;
                }
            }

            return output;
        }

        public static string[] GenerateName(List<Property> paramSet)
        {
            string[] name = new string[paramSet.Count()];

            for (int i = 0; i < paramSet.Count; i++)
            {
                name[i] = paramSet[i].ReadmeValue.ToString();
            }
            if (name == null)
            {
                name = new string[1]
                    {
                        "NoParametersSet"
                    };
            }
            return name;
        }

        /// <summary>
        /// Takes a string and puts spaces before capitals to make it more human readable.
        /// </summary>
        /// <returns>String with added spaces</returns>
        //https://stackoverflow.com/questions/272633/add-spaces-before-capital-letters
        public static string GenerateNameWithSpaces(string sourceName, bool fullName = false)
        {
            StringBuilder name = new StringBuilder();
            name.Append(sourceName[0]);
            for (int i = 1; i < sourceName.Length; i++)
            {
                if (Equals(sourceName[i], '_') && !fullName)
                {
                    break;
                }
                else if (char.IsUpper(sourceName[i]) &&
                    sourceName[i - 1] != ' ' &&
                    !char.IsUpper(sourceName[i - 1]))
                {
                    name.Append(' ');
                }
                else if (char.IsNumber(sourceName[i]))
                {
                    name.Append(' ');
                }

                if (!Equals(sourceName[i], '_'))
                {
                    if (char.IsUpper(sourceName[i]) &&
                        name.Length > 0 &&
                        char.IsUpper(sourceName[i - 1]))
                    {
                        name.Append(char.ToLower(sourceName[i]));
                    }
                    else
                    {
                        name.Append(sourceName[i]);
                    }
                }
            }

            var output = name.ToString().Replace("Uv", "UV");

            return output;
        }

        public static string GenerateNonbinaryName(string sourceName)
        {
            StringBuilder name = new StringBuilder();
            bool beginningFound = false;
            for (int i = 0; i < sourceName.Length; i++)
            {
                if (beginningFound)
                {
                    if (Equals(sourceName[i], '_'))
                    {
                        name.Append(' ');
                    }
                    else if (char.IsUpper(sourceName[i]))
                    {
                        name.Append(' ');
                        name.Append(sourceName[i]);
                    }
                    else
                    {
                        name.Append(sourceName[i]);
                    }
                }
                if (Equals(sourceName[i], '_'))
                {
                    beginningFound = true;
                    name.Append(sourceName[i + 1]); // Avoids starting with a space
                    i++;
                }
            }

            return name.ToString();
        }
    }
}
