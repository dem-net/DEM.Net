using DEM.Net.Core;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace DEM.Net.glTF.SharpglTF
{

    public class SharpGltfService
    {
        public ModelRoot GenerateModel(IEnumerable<AssetGenerator.Runtime.MeshPrimitive> meshPrimitives, string name, IEnumerable<Attribution> attributions = null)
        {


            foreach (var meshPrimitive in meshPrimitives)
            {
                var material1 = new MaterialBuilder()
                              .WithDoubleSide(true)
                              .WithMetallicRoughnessShader();

                if (meshPrimitive.Material?.MetallicRoughnessMaterial != null)
                {

                    //if (meshPrimitive.Material?.MetallicRoughnessMaterial.BaseColorTexture != null)
                    //{
                    //    material1 = material1.WithChannelImage(KnownChannel.BaseColor, meshPrimitive.Material.MetallicRoughnessMaterial.BaseColorTexture.Source.Uri);
                    //}
                    //if (meshPrimitive.Material?.NormalTexture != null)
                    //{
                    //    material1 = material1.WithNormal(meshPrimitive.Material.NormalTexture.Source.Uri);
                    //}
                    //var meshBuilder = new MeshBuilder<VertexPositionNormal, VertexColor1Texture1>("DEMNetModel");
                    //var mesh = meshBuilder.UsePrimitive(material1);
                    //mesh.AddQuadrangle()


                }
            }

            return null;
            //ModelRoot model = ModelRoot.CreateModel()
        }
    }
}
