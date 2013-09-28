using System;
using System.Collections.Generic;
using System.Drawing;
using Lattice;

namespace LinkGraph {

    class WorldModel {

        /// <summary>
        /// The pen used to draw edges. 
        /// </summary>
        private static readonly Pen EdgePen = new Pen(Color.LightGray);

        /// <summary>
        /// The number of nodes in the model. 
        /// </summary>
        public int NodeCount {
            get {
                return _nodes.Count;
            }
        }

        /// <summary>
        /// The number of edges in the model. 
        /// </summary>
        public int EdgeCount {
            get {
                return _edges.Count;
            }
        }

        /// <summary>
        /// The 3D renderer.
        /// </summary>
        private Renderer _renderer = new Renderer();

        /// <summary>
        /// The collection of nodes in the model. 
        /// </summary>
        private List<Node> _nodes = new List<Node>();

        /// <summary>
        /// The collection of edges in the model. 
        /// </summary>
        private List<Tuple<Node, Node>> _edges = new List<Tuple<Node, Node>>();

        /// <summary>
        /// Adds a node to the model. 
        /// </summary>
        /// <param name="node">The node to add to the model.</param>
        public void Add(Node node) {
            _nodes.Add(node);
        }

        /// <summary>
        /// Adds a collection of nodes to the model.
        /// </summary>
        /// <param name="nodes">The collection of nodes to add to the model</param>
        public void AddRange(IEnumerable<Node> nodes) {
            _nodes.AddRange(nodes);
        }

        /// <summary>
        /// Connects two nodes in the model. 
        /// </summary>
        /// <param name="a">A node to connect.</param>
        /// <param name="b">A node to connect.</param>
        public void Connect(Node a, Node b) {
            a.Connected.Add(b);
            b.Connected.Add(a);
            _edges.Add(new Tuple<Node, Node>(a, b));
        }

        /// <summary>
        /// Updates the model. 
        /// </summary>
        public void Update() {
            // TODO: implement this method. 
        }

        /// <summary>
        /// Draws the model. 
        /// </summary>
        /// <param name="g">The graphics surface.</param>
        /// <param name="showLabels">Whether to draw node labels.</param>
        public void Draw(Graphics g, bool showLabels = true) {

            // Draw edges. 
            for (int i = 0; i < _edges.Count; i++) {
                Tuple<Node, Node> edge = _edges[i];
                g.DrawLine(EdgePen, _renderer.ComputePoint(edge.Item1.Location), _renderer.ComputePoint(edge.Item2.Location));
            }

            // Draw nodes.
            for (int i = 0; i < _nodes.Count; i++)
                _nodes[i].Draw(_renderer, g, showLabels);
        }
    }
}
