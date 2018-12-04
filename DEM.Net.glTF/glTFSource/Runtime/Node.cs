using System;
using System.Collections.Generic;
using System.Numerics;

namespace AssetGenerator.Runtime
{
    /// <summary>
    /// A node in a node hierarchy.  
    /// </summary>
    public class Node
    {
        /// <summary>
        /// A floating-point 4x4 transformation matrix stored in column-major order.
        /// </summary>
        public Matrix4x4? Matrix { get; set; }

        /// <summary>
        /// The mesh in this node.
        /// </summary>
        public Mesh Mesh { get; set; }

        /// <summary>
        /// The node's unit quaternion rotation in the order (x, y, z, w), where w is the scalar.
        /// </summary>
        public Quaternion? Rotation { get; set; }

        /// <summary>
        /// The node's non-uniform scale
        /// </summary>
        public Vector3? Scale { get; set; }

        /// <summary>
        /// The node's translation
        /// </summary>
        public Vector3? Translation { get; set; }

        /// <summary>
        /// children of this node.
        /// </summary>
        public IEnumerable<Node> Children { get; set; }

        public Skin Skin { get; set; }

        /// <summary>
        /// Name of the node
        /// </summary>
        public string Name { get; set; }
    }
}
