#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace AssetGenerator.Runtime
{
    public class SkinJoint
    {
        public Matrix4x4 InverseBindMatrix;
        public Node Node;

        public SkinJoint(Matrix4x4 inverseBindMatrix, Node node)
        {
            InverseBindMatrix = inverseBindMatrix;
            Node = node;
        }
    }
}

#pragma warning restore 1591