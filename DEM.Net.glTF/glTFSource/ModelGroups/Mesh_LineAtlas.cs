using LineAtlas.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AssetGenerator
{
	public class Mesh_LineAtlas : ModelGroup
	{
		public override ModelGroupId Id => ModelGroupId.Mesh_LineAtlas;

		//public Mesh_LineAtlas(List<ProjectedPointColor> lineAtlas)
		//{
		//	// RGB colors 4th component is mistery to me...
		//	IEnumerable<Vector4> vertexColors = lineAtlas.Select(pt => new Vector4(pt.Color.R / 255f
		//		, pt.Color.G / 255f
		//		, pt.Color.B / 255f
		//		, 0));

		//	IEnumerable<Vector3> positions = lineAtlas.Select(pt => new Vector3((float)pt.ProjectedPoint.X
		//		, (float)pt.ProjectedPoint.Y
		//		, (float)pt.ProjectedPoint.Z));

		//	Model CreateModel(Action<List<Property>, Runtime.MeshPrimitive> setProperties)
		//	{
		//		var properties = new List<Property>();
		//		var meshPrimitive = new Runtime.MeshPrimitive()
		//		{
		//			Colors = vertexColors
		//		,
		//			ColorComponentType = Runtime.MeshPrimitive.ColorComponentTypeEnum.FLOAT
		//		,
		//			ColorType = Runtime.MeshPrimitive.ColorTypeEnum.VEC3
		//		,
		//			Mode = Runtime.MeshPrimitive.ModeEnum.LINES
		//		,
		//			Positions = positions
		//		};

		//		meshPrimitive.Material = new Runtime.Material();


		//		// Apply the properties that are specific to this gltf.
		//		setProperties(properties, meshPrimitive);

		//		// Create the gltf object
		//		return new Model
		//		{
		//			Properties = properties,
		//			GLTF = CreateGLTF(() => new Runtime.Scene()
		//			{
		//				Nodes = new List<Runtime.Node>
		//				{
		//					new Runtime.Node
		//					{
		//						Mesh = new Runtime.Mesh
		//						{
		//							MeshPrimitives = new List<Runtime.MeshPrimitive>
		//							{
		//								meshPrimitive
		//							}
		//						},
		//					},
		//				},
		//			}),
		//		};
		//	}

		//	void SetVertexColorVec3Float(List<Property> properties, Runtime.MeshPrimitive meshPrimitive)
		//	{
		//		meshPrimitive.ColorComponentType = Runtime.MeshPrimitive.ColorComponentTypeEnum.FLOAT;
		//		meshPrimitive.ColorType = Runtime.MeshPrimitive.ColorTypeEnum.VEC3;

		//		properties.Add(new Property(PropertyName.VertexColor, "Vector3 Float"));
		//	}

		//	this.Models = new List<Model>
		//	{

		//		CreateModel((properties, meshPrimitive) => {
		//			SetVertexColorVec3Float(properties, meshPrimitive);
		//		})
		//	};

		//	GenerateUsedPropertiesList();
		//}
	}
}
