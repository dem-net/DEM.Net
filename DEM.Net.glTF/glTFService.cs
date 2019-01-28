using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.Lib;
using DEM.Net.Lib.Imagery;
using DEM.Net.Lib.Services.Mesh;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.glTF
{
    public class glTFService : IglTFService
    {
        public void Export(Model model, string outputFolder, string modelName, bool exportglTF = true, bool exportGLB = true)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                Directory.CreateDirectory(outputFolder);
                var jsonSerializer = new Newtonsoft.Json.JsonSerializer
                {
                    Formatting = Newtonsoft.Json.Formatting.Indented,
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                };


                var filename = string.Concat(modelName, ".gltf");
                var glbFilename = string.Concat(modelName, ".glb");
                var dataFileName = string.Concat(modelName, ".bin");

                using (var data = new Data(dataFileName))
                {
                    // Passes the desired properties to the runtime layer, which then coverts that data into
                    // a gltf loader object, ready to create the model
                    var converter = new GLTFConverter { CreateInstanceOverride = model.CreateSchemaInstance };
                    var gltf = converter.ConvertRuntimeToSchema(model.GLTF, data);

                    // Makes last second changes to the model that bypass the runtime layer
                    // in order to add features that don't really exist otherwise
                    model.PostRuntimeChanges(gltf);

                    var assetFile = Path.Combine(outputFolder, filename);
                    var glbBinChunck = data.ToArray();

                    if (exportglTF)
                    {
                        // Creates the .gltf file and writes the model's data to it
                        glTFLoader.Interface.SaveModel(gltf, assetFile);

                        // Creates the .bin file and writes the model's data to it
                        var dataFile = Path.Combine(outputFolder, data.Name);
                        File.WriteAllBytes(dataFile, glbBinChunck);
                    }

                    if (exportGLB)
                    {
                        var glbFile = Path.Combine(outputFolder, glbFilename);

                        glTFLoader.Interface.SaveBinaryModelPacked(gltf, glbFile, assetFile, glbBinChunck);
                    }
                }

                Console.WriteLine("Model Creation Complete!");
                Console.WriteLine("Completed in : " + sw.Elapsed.ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                throw;
            }
        }

        public glTFLoader.Schema.Gltf Import(string path)
        {
            glTFLoader.Schema.Gltf gltf = glTFLoader.Interface.LoadModel(path);
            return gltf;
        }
        public Model GenerateModel(MeshPrimitive meshPrimitive, string name)
        {
            // Create the gltf object
            Model model = new Model
            {
                GLTF = new GLTF
                {
                    Asset = new Asset
                    {
                        Generator = "glTF Asset Generator",
                        Version = "2.0",
                    },
                    Scenes = new List<Scene>
                    {
                        new Scene {
                        Nodes = new List<Node>
                        {
                            new Node
                            {
                                Mesh = new Mesh
                                {
                                    MeshPrimitives = new List<MeshPrimitive>
                                    {
                                        meshPrimitive
                                    }
                                },
                            },
                        },
                        }
                    }
                }
            };

            return model;
        }

        public Model GenerateModel(IEnumerable<MeshPrimitive> meshPrimitives, string name)
        {
            // Create the gltf object
            Model model = new Model
            {
                GLTF = new GLTF
                {
                    Asset = new Asset
                    {
                        Generator = "glTF Asset Generator",
                        Version = "2.0",
                    },
                    Scenes = new List<Scene>
                    {
                        new Scene {
                        Nodes = new List<Node>
                        {
                            new Node
                            {
                                Mesh = new Mesh
                                {
                                    MeshPrimitives = meshPrimitives
                                },
                            },
                        },
                        }
                    }
                }
            };

            return model;
        }


        #region Mesh generation (triangles, lines, points)
        public IEnumerable<MeshPrimitive> GetHeightPlanes()
        {
            float size = 0.5f;
            MeshPrimitive mesh = CreateEmptyTriangleMesh();
            float y = -size;
            List<Vector3> positions = new List<Vector3>
            {
               new Vector3(-size,y,-size)
               ,new Vector3(-size,y,size)
               ,new Vector3(size,y,size)
               ,new Vector3(size,y,-size)
            };
            mesh.Positions = positions;
            mesh.Colors = positions.Select(n => new Vector4(1, 0, 0, 0));
            mesh.Indices = new int[] { 0, 1, 3, 1, 2, 3 };
            mesh.Normals = MeshService.ComputeNormals(positions, mesh.Indices.ToList());
            yield return mesh;

            //=====================
            mesh = CreateEmptyTriangleMesh();
            y = 0;
            positions = new List<Vector3>
            {
                new Vector3(-size,y,-size)
               ,new Vector3(-size,y,size)
               ,new Vector3(size,y,size)
               ,new Vector3(size,y,-size)
            };
            mesh.Positions = positions;
            mesh.Colors = positions.Select(n => new Vector4(0, 1, 0, 0));
            mesh.Indices = new int[] { 0, 1, 3, 1, 2, 3 };
            mesh.Normals = MeshService.ComputeNormals(positions, mesh.Indices.ToList());
            yield return mesh;

            //=====================
            mesh = CreateEmptyTriangleMesh();
            y = size;
            positions = new List<Vector3>
            {
               new Vector3(-size,y,-size)
               ,new Vector3(-size,y,size)
               ,new Vector3(size,y,size)
               ,new Vector3(size,y,-size)
            };
            mesh.Positions = positions;
            mesh.Colors = positions.Select(n => new Vector4(0, 0, 1, 0));
            mesh.Indices = new int[] { 0, 1, 3, 1, 2, 3 };
            mesh.Normals = MeshService.ComputeNormals(positions, mesh.Indices.ToList());
            yield return mesh;

        }

        private MeshPrimitive CreateEmptyTriangleMesh()
        {
            return new MeshPrimitive
            {
                Mode = MeshPrimitive.ModeEnum.TRIANGLES
                ,
                ColorComponentType = MeshPrimitive.ColorComponentTypeEnum.FLOAT
                 ,
                IndexComponentType = MeshPrimitive.IndexComponentTypeEnum.UNSIGNED_INT
                 ,
                ColorType = MeshPrimitive.ColorTypeEnum.VEC3
                 ,
                Material = new Material()
                 ,
            };
        }

        /// <summary>
        /// Generate a triangle mesh from supplied height map, triangulating and optionaly mapping UVs
        /// </summary>
        /// <param name="heightMap"></param>
        /// <param name="colors"></param>
        /// <param name="texture">Texture path relative from the model</param>
        /// <returns></returns>
        public MeshPrimitive GenerateTriangleMesh(HeightMap heightMap, IEnumerable<Vector4> colors = null, PBRTexture texture = null)
        {
            List<int> indices = MeshService.TriangulateHeightMap(heightMap).ToList();
            return GenerateTriangleMesh(heightMap.Coordinates, indices, colors, texture);
        }

        public MeshPrimitive GenerateLine(IEnumerable<GeoPoint> points, Vector4 color, float width)
        {
            MeshPrimitive mesh = null;
            try
            {
                if (points == null)
                {
                    Logger.Warning("Points are empty.");
                }
                else
                {
                    if (width == 0)
                    {
                        // Basic line strip  declaration
                        mesh = new MeshPrimitive()
                        {
                            Colors = points.Select(c => color)
                            ,
                            ColorComponentType = MeshPrimitive.ColorComponentTypeEnum.FLOAT
                            ,
                            ColorType = MeshPrimitive.ColorTypeEnum.VEC4
                            ,
                            Mode = MeshPrimitive.ModeEnum.LINE_STRIP
                            ,
                            Positions = points.Select(pt => pt.ToVector3())
                            ,
                            Material = new Material()
                        };
                    }
                    else
                    {
                        // https://gist.github.com/gszauer/5718441
                        // Line triangle mesh
                        var sections = points.Select(pt => pt.ToVector3())
                            .Distinct()
                            .ToList();

                        List<Vector3> vertices = new List<Vector3>(sections.Count * 2);

                        for (int i = 0; i < sections.Count - 1; i++)
                        {
                            Vector3 current = sections[i];
                            Vector3 next = sections[i + 1];
                            Vector3 dir = Vector3.Normalize(next - current);


                            // translate the vector to the left along its way
                            Vector3 side = Vector3.Cross(dir, Vector3.UnitY) * width;

                            Vector3 v0 = current - side; // 0
                            Vector3 v1 = current + side; // 1

                            vertices.Add(v0);
                            vertices.Add(v1);

                            if (i == sections.Count - 2) // add last vertices
                            {
                                v0 = next - side; // 0
                                v1 = next + side; // 1
                                vertices.Add(v0);
                                vertices.Add(v1);
                            }
                        }
                        // add last vertices


                        List<int> indices = new List<int>((sections.Count - 1) * 6);
                        for (int i = 0; i < sections.Count - 1; i++)
                        {
                            int i0 = i * 2;
                            indices.Add(i0);
                            indices.Add(i0 + 1);
                            indices.Add(i0 + 3);

                            indices.Add(i0 + 0);
                            indices.Add(i0 + 3);
                            indices.Add(i0 + 2);
                        }

                        IEnumerable<Vector3> normals = MeshService.ComputeNormals(vertices, indices);
                        // Basic line strip  declaration
                        mesh = new MeshPrimitive()
                        {
                            Colors = vertices.Select(v => color)
                            ,
                            ColorComponentType = MeshPrimitive.ColorComponentTypeEnum.FLOAT
                            ,
                            ColorType = MeshPrimitive.ColorTypeEnum.VEC4
                            ,
                            Mode = MeshPrimitive.ModeEnum.TRIANGLES
                            ,
                            Positions = vertices
                            ,
                            Material = new Material() { DoubleSided = true }
                            ,
                            Indices = indices
                            ,
                            Normals = normals
                            ,
                            IndexComponentType = MeshPrimitive.IndexComponentTypeEnum.UNSIGNED_INT
                        };


                    }



                }


            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                throw;
            }
            return mesh;
        }

        public MeshPrimitive GenerateTriangleMesh(IEnumerable<GeoPoint> points, List<int> indices, IEnumerable<Vector4> colors = null, PBRTexture texture = null)
        {
            return GenerateTriangleMesh(points.ToVector3(), indices, colors, texture);
        }
        public MeshPrimitive GenerateTriangleMesh(IEnumerable<Vector3> points, List<int> indices, IEnumerable<Vector4> colors = null, PBRTexture texture = null)
        {
            MeshPrimitive mesh = null;
            const int TRIANGULATION_MODE = 1; // 2
            try
            {
                if (points == null || !points.Any())
                {
                    Logger.Warning("Vertex list is empty.");
                }
                else
                {

                    List<Vector3> positions = points.ToList();
                    if (colors == null)
                    {
                        colors = positions.Select(pt => Vector4.One);
                    }

                    // Basic mesh declaration
                    mesh = new MeshPrimitive()
                    {
                        Colors = colors
                        ,
                        ColorComponentType = MeshPrimitive.ColorComponentTypeEnum.FLOAT
                        ,
                        ColorType = MeshPrimitive.ColorTypeEnum.VEC3
                        ,
                        Mode = MeshPrimitive.ModeEnum.TRIANGLES
                        ,
                        Positions = positions
                        ,
                        Material = new Material() { DoubleSided = true }
                        ,
                        Indices = indices
                    };


                    //The number of the vertices
                    int nV = positions.Count;
                    //The number of the triangles
                    int nT = indices.Count / 3;
                    Vector3[] norm = new Vector3[nV]; //Array for the normals
                                                      //Scan all the triangles. For each triangle add its
                                                      //normal to norm's vectors of triangle's vertices
                    Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                    Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                    for (int t = 0; t < nT; t++)
                    {
                        //Get indices of the triangle t
                        int i1 = indices[3 * t];
                        int i2 = indices[3 * t + 1];
                        int i3 = indices[3 * t + 2];
                        //Get vertices of the triangle
                        Vector3 v1 = positions[i1];
                        Vector3 v2 = positions[i2];
                        Vector3 v3 = positions[i3];

                        //Compute the triangle's normal
                        Vector3 dir = Vector3.Normalize(Vector3.Cross(v2 - v1, v3 - v1));
                        //Accumulate it to norm array for i1, i2, i3
                        norm[i1] += dir;
                        norm[i2] += dir;
                        norm[i3] += dir;
                    }

                    for (int i = 0; i < nV; i++)
                    {
                        //Normalize the normal's length
                        norm[i] = Vector3.Normalize(norm[i]);

                        // Calculate bounds of UV mapping
                        var pos = positions[i];

                        // for UV coords
                        min.X = Math.Min(pos.X, min.X);
                        min.Y = Math.Min(pos.Y, min.Y);
                        min.Z = Math.Min(pos.Z, min.Z);

                        max.X = Math.Max(pos.X, max.X);
                        max.Y = Math.Max(pos.Y, max.Y);
                        max.Z = Math.Max(pos.Z, max.Z);
                    }
                    mesh.Normals = norm;

                    if (texture != null)
                    {
                        mesh.TextureCoordsComponentType = MeshPrimitive.TextureCoordsComponentTypeEnum.FLOAT;
                        if (texture.TextureCoordSets == null)
                        {
                            mesh.TextureCoordSets = Enumerable.Range(0, 1).Select(i => positions.Select(pos => new Vector2(
                                MathHelper.Map(min.X, max.X, 0, 1, pos.X, true)
                                , MathHelper.Map(min.Z, max.Z, 0, 1, pos.Z, true)
                                )));
                        }
                        else
                        {
                            mesh.TextureCoordSets = Enumerable.Range(0, 1).Select(i => texture.TextureCoordSets);
                        }
                        mesh.Material.MetallicRoughnessMaterial = new PbrMetallicRoughness()
                        {
                            BaseColorFactor = Vector4.One,
                            BaseColorTexture = GetTextureFromImage(texture.BaseColorTexture.FilePath),
                            MetallicFactor = 0,
                        };
                        if (texture.NormalTexture != null)
                        {
                            mesh.Material.NormalTexture = GetTextureFromImage(texture.NormalTexture.FilePath);
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                throw;
            }
            return mesh;
        }

        private Texture GetTextureFromImage(string texture)
        {
            if (!File.Exists(texture))
                throw new ArgumentException("Texture file does not exists");

            var mimeType = Path.GetExtension(texture.ToLower()).EndsWith("png")
                                ? glTFLoader.Schema.Image.MimeTypeEnum.image_png
                                : glTFLoader.Schema.Image.MimeTypeEnum.image_jpeg;

            return new Texture
            {
                Source = new Image()
                {
                    MimeType = mimeType,
                    Name = Path.GetFileNameWithoutExtension(texture),
                    Uri = Path.GetFileName(texture) // relative path
                }
            };
        }

        public MeshPrimitive GeneratePointMesh(IEnumerable<GeoPoint> points, Vector4 color, float pointSize)
        {
            MeshPrimitive mesh = null;
            try
            {
                if (points == null)
                {
                    Logger.Warning("Points are empty.");
                }
                else
                {
                    if (pointSize == 0)
                    {
                        // Basic point declaration
                        mesh = new MeshPrimitive()
                        {
                            Colors = points.Select(c => color)
                            ,
                            ColorComponentType = MeshPrimitive.ColorComponentTypeEnum.FLOAT
                            ,
                            ColorType = MeshPrimitive.ColorTypeEnum.VEC3
                            ,
                            Mode = MeshPrimitive.ModeEnum.POINTS
                            ,
                            Positions = points.Select(pt => pt.ToVector3())
                            ,
                            Material = new Material()
                        };
                    }
                    else
                    {
                        // points interpreted as quads where point is at the quad center
                        // Basic point declaration
                        var vecs = points.ToVector3().ToList();
                        var deltaZ = vecs.Max(p => p.Z) - vecs.Min(p => p.Z);
                        var deltaX = vecs.Max(p => p.X) - vecs.Min(p => p.X);
                        pointSize = (deltaX * 0.5f) / (float)Math.Sqrt(vecs.Count);
                        IEnumerable<Vector3> vertices = points.ToVector3().SelectMany(v => v.ToQuadPoints(pointSize));
                        List<int> indices = Enumerable.Range(0, points.Count()).SelectMany(i => VectorsExtensions.TriangulateQuadIndices(i * 4)).ToList();
                        mesh = GenerateTriangleMesh(vertices, indices, null);
                    }



                }


            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                throw;
            }
            return mesh;
        }
        #endregion



    }
}
