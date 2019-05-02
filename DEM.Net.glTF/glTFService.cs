﻿//
// glTFService.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.Core;
using DEM.Net.Core.Imagery;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;

namespace DEM.Net.glTF
{
    public class glTFService : IglTFService
    {
        private readonly ILogger<glTFService> _logger;
        private IMeshService _meshService;

        public glTFService(IMeshService meshService, ILogger<glTFService> logger = null)
        {
            _logger = logger;
            _meshService = meshService;
        }
        public void Export(Model model, string outputFolder, string modelName, bool exportglTF = true, bool exportGLB = true)
        {
            try
            {
                Stopwatch sw = Stopwatch.StartNew();

                Directory.CreateDirectory(outputFolder);
                Newtonsoft.Json.JsonSerializer jsonSerializer = new Newtonsoft.Json.JsonSerializer
                {
                    Formatting = Newtonsoft.Json.Formatting.Indented,
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                };


                string filename = string.Concat(modelName, ".gltf");
                string glbFilename = string.Concat(modelName, ".glb");
                string dataFileName = string.Concat(modelName, ".bin");

                using (Data data = new Data(dataFileName))
                {
                    // Passes the desired properties to the runtime layer, which then coverts that data into
                    // a gltf loader object, ready to create the model
                    GLTFConverter converter = new GLTFConverter { CreateInstanceOverride = model.CreateSchemaInstance };
                    glTFLoader.Schema.Gltf gltf = converter.ConvertRuntimeToSchema(model.GLTF, data);

                    // Makes last second changes to the model that bypass the runtime layer
                    // in order to add features that don't really exist otherwise
                    model.PostRuntimeChanges(gltf);

                    string assetFile = Path.Combine(outputFolder, filename);
                    byte[] glbBinChunck = data.ToArray();

                    if (exportglTF)
                    {
                        // Creates the .gltf file and writes the model's data to it
                        glTFLoader.Interface.SaveModel(gltf, assetFile);

                        // Creates the .bin file and writes the model's data to it
                        string dataFile = Path.Combine(outputFolder, data.Name);
                        File.WriteAllBytes(dataFile, glbBinChunck);
                    }

                    if (exportGLB)
                    {
                        string glbFile = Path.Combine(outputFolder, glbFilename);

                        glTFLoader.Interface.SaveBinaryModelPacked(gltf, glbFile, assetFile, glbBinChunck);
                    }
                }

                Console.WriteLine("Model Creation Complete!");
                Console.WriteLine("Completed in : " + sw.Elapsed.ToString());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, ex.Message);
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
            mesh.Normals = _meshService.ComputeNormals(positions, mesh.Indices.ToList());
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
            mesh.Normals = _meshService.ComputeNormals(positions, mesh.Indices.ToList());
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
            mesh.Normals = _meshService.ComputeNormals(positions, mesh.Indices.ToList());
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
            Triangulation triangulation = _meshService.TriangulateHeightMap(heightMap);
            return GenerateTriangleMesh(triangulation, colors, texture);
        }
        /// <summary>
        /// Generate a triangle mesh from supplied height map, triangulating and optionaly mapping UVs
        /// and generate sides and bottom (like a box where the top is the triangulated height map)
        /// </summary>
        /// <param name="heightMap">Height map.</param>
        /// <param name="thickness">Determines how box height will be calculated</param>
        /// <param name="zValue">Z value to apply for box calculation</param>
        /// <returns></returns>
        public MeshPrimitive GenerateTriangleMesh_Boxed(HeightMap heightMap, BoxBaseThickness thickness = BoxBaseThickness.FixedElevation, float zValue = 0f)
        {
            Triangulation triangulation = _meshService.GenerateTriangleMesh_Boxed(heightMap, thickness, zValue);

            return GenerateTriangleMesh(triangulation);
        }

        public MeshPrimitive GenerateLine(IEnumerable<GeoPoint> points, Vector4 color, float width)
        {
            MeshPrimitive mesh = null;
            try
            {
                if (points == null)
                {
                    _logger?.LogWarning("Points are empty.");
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
                        List<Vector3> sections = points.Select(pt => pt.ToVector3())
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

                        IEnumerable<Vector3> normals = _meshService.ComputeNormals(vertices, indices);
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
                _logger?.LogError(ex, ex.ToString());
                throw;
            }
            return mesh;
        }

        public MeshPrimitive GenerateTriangleMesh(IEnumerable<GeoPoint> points, List<int> indices, IEnumerable<Vector4> colors = null, PBRTexture texture = null)
        {
            Stopwatch sw = null;
            if ((_logger?.IsEnabled(LogLevel.Trace)).GetValueOrDefault(false))
            {
                 sw = Stopwatch.StartNew();
                _logger.LogTrace("Baking points...");
            }

            var pointsList = points.ToVector3().ToList();
            MeshPrimitive mesh = GenerateTriangleMesh(pointsList, indices, colors, texture);

            if ((_logger?.IsEnabled(LogLevel.Trace)).GetValueOrDefault(false))
            {
                sw.Stop();
                _logger.LogTrace($"Baking points done in {sw.Elapsed:g}");
            }

            return mesh;
        }
        public MeshPrimitive GenerateTriangleMesh(Triangulation triangulation, IEnumerable<Vector4> colors = null, PBRTexture texture = null)
        {
            return GenerateTriangleMesh(triangulation.Positions, triangulation.Indices.ToList(), colors, texture);
        }
        public MeshPrimitive GenerateTriangleMesh(List<Vector3> positions, List<int> indices, IEnumerable<Vector4> colors = null, PBRTexture texture = null)
        {
            MeshPrimitive mesh = null;
            try
            {
                if (positions == null || !positions.Any())
                {
                    _logger?.LogWarning("Vertex list is empty.");
                }
                else
                {

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
                        Vector3 pos = positions[i];

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
                _logger?.LogError(ex, ex.ToString());
                throw;
            }
            return mesh;
        }
      

        private Texture GetTextureFromImage(string texture)
        {
            if (!File.Exists(texture))
            {
                throw new ArgumentException("Texture file does not exists");
            }

            glTFLoader.Schema.Image.MimeTypeEnum mimeType = Path.GetExtension(texture.ToLower()).EndsWith("png")
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

        #endregion



    }
}
