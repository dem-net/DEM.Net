using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DEM.Net.Core.Graph
{
    /// <summary>
    /// Source https://stackoverflow.com/a/15310845/1818237
    /// </summary>
    public class Node<T>
    {
        public T Model;
        public string Key;
        public List<Arc<T>> Arcs = new List<Arc<T>>();

        public Node(T model, string key)
        {
            Model = model;
            Key = key;
        }

        public override string ToString()
        {
            return string.Concat(Key, " (", Arcs.Count, " arc(s))");
        }

        /// <summary>
        /// Create a new arc, connecting this Node to the Nod passed in the parameter
        /// Also, it creates the inversed node in the passed node
        /// </summary>
        public Node<T> AddArc(Node<T> child, float w, bool biDirectional = false)
        {
            Arcs.Add(new Arc<T>
            {
                Parent = this,
                Child = child,
                Weigth = w
            });

            if (biDirectional && !child.Arcs.Exists(a => a.Parent == child && a.Child == this))
            {
                child.AddArc(this, w);
            }

            return this;
        }
    }

    public class Arc<T>
    {
        public float Weigth;
        public Node<T> Parent;
        public Node<T> Child;

        public bool Visited { get; internal set; }

        public override string ToString()
        {
            return $"{Parent.Key} -> {Child.Key} ({Weigth})";
        }
    }

    public class Graph<T>
    {
        public Node<T> Root;
        public List<Node<T>> AllNodes = new List<Node<T>>();

        public Node<T> CreateRoot(T node, string key)
        {
            Root = CreateNode(node, key);
            return Root;
        }

        public Node<T> CreateNode(T node, string key)
        {
            var n = new Node<T>(node, key);
            AllNodes.Add(n);
            return n;
        }

        internal void ResetVisits()
        {
            foreach(var n in AllNodes)
            {
                foreach(var a in n.Arcs)
                {
                    a.Visited = false;
                }
            }
        }

        //public float?[,] CreateAdjMatrix()
        //{
        //    float?[,] adj = new float?[AllNodes.Count, AllNodes.Count];

        //    for (int i = 0; i < AllNodes.Count; i++)
        //    {
        //        Node n1 = AllNodes[i];

        //        for (int j = 0; j < AllNodes.Count; j++)
        //        {
        //            Node n2 = AllNodes[j];

        //            var arc = n1.Arcs.FirstOrDefault(a => a.Child == n2);

        //            if (arc != null)
        //            {
        //                adj[i, j] = arc.Weigth;
        //            }
        //        }
        //    }
        //    return adj;
        //}
    }

}