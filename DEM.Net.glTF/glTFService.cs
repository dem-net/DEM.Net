using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.Lib;
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


                var filename = $"{modelName}.gltf";
                var glbFilename = $"{modelName}.glb";

                using (var data = new Data($"{modelName}.bin"))
                {
                    // Passes the desired properties to the runtime layer, which then coverts that data into
                    // a gltf loader object, ready to create the model
                    var converter = new GLTFConverter { CreateInstanceOverride = model.CreateSchemaInstance };
                    var gltf = converter.ConvertRuntimeToSchema(model.GLTF, data);

                    // Makes last second changes to the model that bypass the runtime layer
                    // in order to add features that don't really exist otherwise
                    model.PostRuntimeChanges(gltf);

                    if (exportglTF)
                    {
                        // Creates the .gltf file and writes the model's data to it
                        var assetFile = Path.Combine(outputFolder, filename);
                        glTFLoader.Interface.SaveModel(gltf, assetFile);


                        // Creates the .bin file and writes the model's data to it
                        var dataFile = Path.Combine(outputFolder, data.Name);
                        File.WriteAllBytes(dataFile, data.ToArray());
                    }

                    if (exportGLB)
                    {
                        var glbFile = Path.Combine(outputFolder, glbFilename);
                        foreach (var buf in gltf.Buffers)
                        {
                            buf.Uri = null;
                        }
                        // gltf.Buffers = null;
                        glTFLoader.Interface.SaveBinaryModel(gltf, data.ToArray(), glbFile);
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
            mesh.Normals = ComputeNormals(positions, mesh.Indices.ToList());
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
            mesh.Normals = ComputeNormals(positions, mesh.Indices.ToList());
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
            mesh.Normals = ComputeNormals(positions, mesh.Indices.ToList());
            yield return mesh;

        }

        private Vector3[] ComputeNormals(List<Vector3> positions, List<int> indices)
        {

            //The number of the vertices
            int nV = positions.Count;
            //The number of the triangles
            int nT = indices.Count / 3;

            Vector3[] norm = new Vector3[nV]; //Array for the normals
                                              //Scan all the triangles. For each triangle add its
                                              //normal to norm's vectors of triangle's vertices
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
            //Normalize the normal's length
            for (int i = 0; i < nV; i++)
            {
                norm[i] = Vector3.Normalize(norm[i]);
            }
            return norm;
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

        public MeshPrimitive GenerateTriangleMesh(HeightMap heightMap, IEnumerable<Vector3> colors = null)
        {
            const int TRIANGULATION_MODE = 1;
            int capacity = ((heightMap.Width - 1) * 6) * (heightMap.Height - 1);
            List<int> indices = new List<int>(capacity);
            // Triangulate mesh -- anti clockwise winding
            for (int y = 0; y < heightMap.Height; y++)
            {
                for (int x = 0; x < heightMap.Width; x++)
                {
                    //Vector3 pt = ToVector3(heightMap.Coordinates[x + y * heightMap.Width]);
                    //pt.z -= mindepth;
                    //cout << x + y * stride << "-> " << pt << endl;
                    //mesh.addVertex(pt);

                    if (x < (heightMap.Width - 1) && y < (heightMap.Height - 1))
                    {
                        if (TRIANGULATION_MODE == 1)
                        {
                            // Triangulation 1
                            indices.Add((x + 0) + (y + 0) * heightMap.Width);
                            indices.Add((x + 0) + (y + 1) * heightMap.Width);
                            indices.Add((x + 1) + (y + 0) * heightMap.Width);

                            indices.Add((x + 1) + (y + 0) * heightMap.Width);
                            indices.Add((x + 0) + (y + 1) * heightMap.Width);
                            indices.Add((x + 1) + (y + 1) * heightMap.Width);
                        }
                        else
                        {

                            // Triangulation 2
                            indices.Add((x + 0) + (y + 0) * heightMap.Width);
                            indices.Add((x + 1) + (y + 1) * heightMap.Width);
                            indices.Add((x + 0) + (y + 1) * heightMap.Width);

                            indices.Add((x + 0) + (y + 0) * heightMap.Width);
                            indices.Add((x + 1) + (y + 0) * heightMap.Width);
                            indices.Add((x + 1) + (y + 1) * heightMap.Width);
                        }
                    }
                }
            }

            return GenerateTriangleMesh(heightMap.Coordinates, indices, colors);
        }

        public MeshPrimitive GenerateLine(IEnumerable<GeoPoint> points, Vector3 color, float width)
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
                            Colors = points.Select(c => color.ToVector4())
                            ,
                            ColorComponentType = MeshPrimitive.ColorComponentTypeEnum.FLOAT
                            ,
                            ColorType = MeshPrimitive.ColorTypeEnum.VEC3
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

                        IEnumerable<Vector3> normals = ComputeNormals(vertices, indices);
                        // Basic line strip  declaration
                        mesh = new MeshPrimitive()
                        {
                            Colors = vertices.Select(v => color.ToVector4())
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

        public MeshPrimitive GenerateTriangleMesh(IEnumerable<GeoPoint> points, List<int> indices, IEnumerable<Vector3> colors = null)
        {
            return GenerateTriangleMesh(points.ToVector3(), indices, colors);
        }
        public MeshPrimitive GenerateTriangleMesh(IEnumerable<Vector3> points, List<int> indices, IEnumerable<Vector3> colors = null)
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
                    if (colors == null)
                    {
                        colors = points.Select(pt => new Vector3(1, 1, 1));
                    }
                    List<Vector3> positions = points.ToList();

                    // Basic mesh declaration
                    mesh = new MeshPrimitive()
                    {
                        Colors = colors.Select(c => c.ToVector4())
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
                    //Normalize the normal's length
                    for (int i = 0; i < nV; i++)
                    {
                        norm[i] = Vector3.Normalize(norm[i]);
                    }
                    mesh.Normals = norm;

                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                throw;
            }
            return mesh;
        }

        public MeshPrimitive GeneratePointMesh(IEnumerable<GeoPoint> points, Vector3 color, float pointSize)
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
                            Colors = points.Select(c => color.ToVector4())
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
