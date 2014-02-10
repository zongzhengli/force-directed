using System;
using System.Drawing;
using Lattice;

namespace ForceDirected {

    /// <summary>
    /// Represents an edge that connects two nodes. 
    /// </summary>
    class Edge {

        /// <summary>
        /// The pen used to draw edges. 
        /// </summary>
        private static readonly Pen EdgePen = new Pen(Color.FromArgb(40, Color.White));

        /// <summary>
        /// The first node connected by the edge. 
        /// </summary>
        public readonly Node Node1;

        /// <summary>
        /// The second node connected by the edge. 
        /// </summary>
        public readonly Node Node2;

        /// <summary>
        /// Constructs an edge that connects two given nodes. 
        /// </summary>
        /// <param name="node1">The first node connected by the edge.</param>
        /// <param name="node2">The second node connected by the edge.</param>
        public Edge(Node node1, Node node2) {
            Node1 = node1;
            Node2 = node2;
        }

        /// <summary>
        /// Draws the edge. 
        /// </summary>
        /// <param name="renderer">The 3D renderer.</param>
        /// <param name="g">The graphics surface.</param>
        public void Draw(Renderer renderer, Graphics g) {
            if (Node1.Location.Z < renderer.Camera.Z || Node2.Location.Z < renderer.Camera.Z)
                g.DrawLine(EdgePen, renderer.ComputePoint(Node1.Location), renderer.ComputePoint(Node2.Location));
        }
    }
}
