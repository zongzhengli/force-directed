using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ForceDirected {

    /// <summary>
    /// Represents an edge that connects two nodes. 
    /// </summary>
    class Edge {

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
    }
}
