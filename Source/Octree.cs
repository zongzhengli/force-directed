using System;
using Lattice;

namespace ForceDirected {

    /// <summary>
    /// Represents the tree structure in the Barnes-Hut algorithm. 
    /// </summary>
    class Octree {

        /// <summary>
        /// The tolerance of the mass grouping approximation. A node is accelerated 
        /// when the ratio of the tree's width to the distance (from the tree's 
        /// center of mass to the Body) is less than this value.
        /// </summary>
        private const double Tolerance = 0.5;

        /// <summary>
        /// The minimum width of a tree. Subtrees are not created if their width 
        /// would be smaller than this value. 
        /// </summary>
        private const double MinimumWidth = 1;

        /// <summary>
        /// The collection of subtrees for the tree. 
        /// </summary>
        private Octree[] _subtrees = null;

        /// <summary>
        /// The location of the center of the tree's bounds. 
        /// </summary>
        private Vector _location;

        /// <summary>
        /// The width of the tree's bounds. 
        /// </summary>
        private double _width = 0;

        /// <summary>
        /// The location of the center of mass of the nodes contained in the tree. 
        /// </summary>
        private Vector _centerOfMass = Vector.Zero;

        /// <summary>
        /// The total mass of the nodes contained in the tree. 
        /// </summary>
        private double _totalMass = 0;

        /// The number of nodes in the tree. This is used to handle special cases 
        /// when there are very few Bodies in the tree. 
        /// </summary>
        private int _nodesCount = 0;

        /// <summary>
        /// The first node added to the tree. This is used when the first node must 
        /// be added to subtrees at a later time. 
        /// </summary>
        private Node _firstNode = null;

        /// <summary>
        /// Constructs a tree with the given width located about the origin.
        /// </summary>
        /// <param name="width">The width of the tree.</param>
        public Octree(double width) {
            _width = width;
        }

        /// <summary>
        /// Constructs a tree with the given location and width.
        /// </summary>
        /// <param name="location">The location of the center of the tree.</param>
        /// <param name="width">The width of the tree.</param>
        public Octree(Vector location, double width)
            : this(width) {
            _location = location;
        }

        /// <summary>
        /// Adds a node to the tree. 
        /// </summary>
        /// <param name="node">The node to add to the tree.</param>
        public void Add(Node node) {
            _centerOfMass = (_totalMass * _centerOfMass + node.Mass * node.Location) / (_totalMass + node.Mass);
            _totalMass += node.Mass;
            _nodesCount++;

            if (_nodesCount == 1)
                _firstNode = node;
            else {
                AddToSubtree(node);
                if (_nodesCount == 2)
                    AddToSubtree(_firstNode);
            }
        }

        /// <summary>
        /// Adds a node to the appropriate subtree based on its position in space. 
        /// </summary>
        /// <param name="node">The node to add to a subtree.</param>
        private void AddToSubtree(Node node) {
            double subtreeWidth = _width / 2;

            // Don't create subtrees if it violates the width limit.
            if (subtreeWidth >= MinimumWidth) {

                if (_subtrees == null)
                    _subtrees = new Octree[8];

                // Determine which subtree the node belongs in and add it to that subtree. 
                int subtreeIndex = 0;
                for (int i = -1; i <= 1; i += 2)
                    for (int j = -1; j <= 1; j += 2)
                        for (int k = -1; k <= 1; k += 2) {
                            Vector subtreeLocation = _location + (subtreeWidth / 2) * new Vector(i, j, k);

                            // Determine if the node is contained within the bounds of the subtree under 
                            // consideration. 
                            if (Math.Abs(subtreeLocation.X - node.Location.X) <= subtreeWidth / 2
                             && Math.Abs(subtreeLocation.Y - node.Location.Y) <= subtreeWidth / 2
                             && Math.Abs(subtreeLocation.Z - node.Location.Z) <= subtreeWidth / 2) {

                                if (_subtrees[subtreeIndex] == null)
                                    _subtrees[subtreeIndex] = new Octree(subtreeLocation, subtreeWidth);
                                _subtrees[subtreeIndex].Add(node);
                                return;
                            }
                            subtreeIndex++;
                        }
            }
        }

        /// <summary>
        /// Updates the acceleration of the given node. 
        /// </summary>
        /// <param name="node">The node to accelerate.</param>
        public void Accelerate(Node node) {
            double dx = _centerOfMass.X - node.Location.X;
            double dy = _centerOfMass.Y - node.Location.Y;
            double dz = _centerOfMass.Z - node.Location.Z;
            double dSquared = dx * dx + dy * dy + dz * dz;

            // Case 1. The tree contains only one node and the given node lies outside 
            //         the bounds of tree. Thus the given node is not the one in the 
            //         tree so we can perform the acceleration. 
            //
            // Case 2. The width to distance ratio is within the defined Tolerance so 
            //         we consider the tree to be effectively a single massive body and 
            //         perform the acceleration. 
            if ((_nodesCount == 1 && (Math.Abs(node.Location.X - _location.X) * 2 > _width
                               || Math.Abs(node.Location.Y - _location.Y) * 2 > _width
                               || Math.Abs(node.Location.Z - _location.Z) * 2 > _width))
             || (_width * _width < Tolerance * Tolerance * dSquared)) {

                // Calculate a normalized acceleration value and multiply it with the 
                // displacement in each coordinate to get that coordinate's acceleration 
                // component. 
                double distance = Math.Sqrt(dSquared + World.RepulsionEpsilon * World.RepulsionEpsilon);
                double normAcc = World.RepulsionFactor * _totalMass / (distance * distance * distance);

                node.Acceleration.X += normAcc * dx;
                node.Acceleration.Y += normAcc * dy;
                node.Acceleration.Z += normAcc * dz;
            }

            // Case 3. More granularity is needed so we give the node to the subtrees. 
            else if (_subtrees != null)
                foreach (Octree subtree in _subtrees)
                    if (subtree != null)
                        subtree.Accelerate(node);
        }
    }
}
