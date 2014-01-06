using System;
using System.Collections.Generic;
using System.Drawing;
using Lattice;
using Threading;

namespace ForceDirected {

    /// <summary>
    /// Represents a world model of nodes and edges. 
    /// </summary>
    class World {

        /// <summary>
        /// The multiplicative factor for origin attraction of nodes. 
        /// </summary>
        public const double OriginFactor = 2e4;

        /// <summary>
        /// The distance softening factor for origin attraction of nodes. 
        /// </summary>
        public const double OriginEpsilon = 7000;

        /// <summary>
        /// The distance within which origin attraction of nodes becomes weaker. 
        /// </summary>
        public const double OriginWeakDistance = 100;

        /// <summary>
        /// The multiplicative factor for repulsion between nodes. 
        /// </summary>
        public const double RepulsionFactor = -900;

        /// <summary>
        /// The distance softening factor for repulsion between nodes. 
        /// </summary>
        public const double RepulsionEpsilon = 2;

        /// <summary>
        /// The multiplicative factor for edge spring stiffness. 
        /// </summary>
        public const double EdgeFactor = 0.1;

        /// <summary>
        /// The ideal length of edges. 
        /// </summary>
        public const double EdgeLength = 30;

        /// <summary>
        /// The default value for the camera's position along the z-axis. 
        /// </summary>
        private const double CameraZDefault = 2000;

        /// <summary>
        /// The multiplicative factor for camera acceleration. 
        /// </summary>
        private const double CameraZAcceleration = -2e-4;

        /// <summary>
        /// The multiplicative factor that gives the rate camera acceleration is 
        /// dampened. 
        /// </summary>
        private const double CameraZEasing = 0.94;

        /// <summary>
        /// The number of nodes in the world model. 
        /// </summary>
        public int NodeCount {
            get {
                return _nodes.Count;
            }
        }

        /// <summary>
        /// The number of edges in the world model. 
        /// </summary>
        public int EdgeCount {
            get {
                return _edges.Count;
            }
        }

        /// <summary>
        /// The number of frames elapsed. 
        /// </summary>
        public long Frames {
            get;
            private set;
        }

        /// <summary>
        /// The 3D renderer.
        /// </summary>
        private Renderer _renderer = new Renderer() {
            Camera = new Vector(0, 0, CameraZDefault),
            FOV = 1400
        };

        /// <summary>
        /// The collection of nodes in the world model. 
        /// </summary>
        private List<Node> _nodes = new List<Node>();

        /// <summary>
        /// The collection of edges in the world model. 
        /// </summary>
        private List<Edge> _edges = new List<Edge>();

        /// <summary>
        /// The lock required to modify the nodes collection. 
        /// </summary>
        private object _nodeLock = new object();

        /// <summary>
        /// The camera's position on the z-axis. 
        /// </summary>
        private double _cameraZ = CameraZDefault;

        /// <summary>
        /// The camera's velocity along the z-axis. 
        /// </summary>
        private double _cameraZVelocity = 0;

        /// <summary>
        /// Constructs a world model. 
        /// </summary>
        public World() {
            Frames = 0;
        }

        /// <summary>
        /// Adds a node to the world model. 
        /// </summary>
        /// <param name="node">The node to add to the world model.</param>
        public void Add(Node node) {
            lock (_nodeLock)
                _nodes.Add(node);
        }

        /// <summary>
        /// Adds a collection of nodes to the world model.
        /// </summary>
        /// <param name="nodes">The collection of nodes to add to the world model.</param>
        public void AddRange(IEnumerable<Node> nodes) {
            lock (_nodeLock)
                _nodes.AddRange(nodes);
        }

        /// <summary>
        /// Connects two nodes in the world model. 
        /// </summary>
        /// <param name="a">A node to connect.</param>
        /// <param name="b">A node to connect.</param>
        public void Connect(Node a, Node b) {
            if (a == b)
                throw new ArgumentException("Cannot connect a node to itself.");
            lock (_nodeLock) {
                a.Connected.Add(b);
                b.Connected.Add(a);
                _edges.Add(new Edge(a, b));
            }
        }

        /// <summary>
        /// Advances the world model by one frame. 
        /// </summary>
        public void Update() {

            // Update nodes.
            lock (_nodeLock) {

                // Update the nodes and determine required tree width. 
                double halfWidth = 0;
                foreach (Node node in _nodes) {
                    node.Update();
                    halfWidth = Math.Max(Math.Abs(node.Location.X), halfWidth);
                    halfWidth = Math.Max(Math.Abs(node.Location.Y), halfWidth);
                    halfWidth = Math.Max(Math.Abs(node.Location.Z), halfWidth);
                }

                // Build tree for node repulsion. 
                Octree tree = new Octree(2.1 * halfWidth);
                foreach (Node node in _nodes)
                    tree.Add(node);

                Parallel.ForEach(_nodes, node => {

                    // Apply repulsion between nodes. 
                    tree.Accelerate(node);

                    // Apply origin attraction of nodes. 
                    Vector originDisplacementUnit = -node.Location.Unit();
                    double originDistance = node.Location.Magnitude();

                    double attractionCofficient = OriginFactor;
                    if (originDistance < OriginWeakDistance)
                        attractionCofficient *= originDistance / OriginWeakDistance;

                    node.Acceleration += originDisplacementUnit * attractionCofficient / (originDistance + OriginEpsilon);

                    // Apply edge spring forces. 
                    foreach (Node other in node.Connected) {
                        Vector displacement = node.Location.To(other.Location);
                        Vector direction = displacement.Unit();
                        double distance = displacement.Magnitude();
                        double idealLength = EdgeLength + node.Radius + other.Radius;

                        node.Acceleration += direction * EdgeFactor * (distance - idealLength) / node.Mass;
                    }
                });

                // Update frame info.
                if (_nodes.Count > 0)
                    Frames++;
            }

            // Update camera.
            _cameraZ += _cameraZVelocity * _cameraZ;
            _cameraZ = Math.Max(1, _cameraZ);
            _cameraZVelocity *= CameraZEasing;
            _renderer.Camera.Z = _cameraZ;
        }

        /// <summary>
        /// Rotates the world model along an arbitrary axis. 
        /// </summary>
        /// <param name="point">The starting point for the axis of rotation.</param>
        /// <param name="direction">The direction for the axis of rotation.</param>
        /// <param name="angle">The angle to rotate by.</param>
        public void Rotate(Vector point, Vector direction, double angle) {
            lock (_nodeLock)
                Parallel.ForEach(_nodes, node => {
                    node.Rotate(point, direction, angle);
                });
        }

        /// <summary>
        /// Moves the camera in association with the given mouse wheel delta. 
        /// </summary>
        /// <param name="delta">The signed number of dents the mouse wheel moved.</param>
        public void MoveCamera(int delta) {
            _cameraZVelocity += delta * CameraZAcceleration;
        }

        /// <summary>
        /// Stops the camera if it is moving. 
        /// </summary>
        public void StopCamera() {
            _cameraZVelocity = 0;
        }

        /// <summary>
        /// Draws the world model. 
        /// </summary>
        /// <param name="g">The graphics surface.</param>
        /// <param name="showLabels">Whether to draw node labels.</param>
        public void Draw(Graphics g, bool showLabels = true) {

            // Draw edges. 
            int edgeCount = _edges.Count;
            for (int i = 0; i < edgeCount; i++) {
                Edge edge = _edges[i];
                if (edge != null)
                    edge.Draw(_renderer, g);
            }

            // Draw nodes.
            int nodeCount = _nodes.Count;
            for (int i = 0; i < nodeCount; i++)
                if (_nodes[i] != null)
                    _nodes[i].Draw(_renderer, g, showLabels);
        }

        /// <summary>
        /// Generates nodes and edges for demonstration. 
        /// </summary>
        public void GenerateDemo() {
            lock (_nodeLock) {
                Color colour = Color.FromArgb(120, Color.White);

                // Add basis nodes. 
                for (int i = 0; i < 100; i++)
                    Add(new Node(colour));

                // Connect some basis nodes. 
                for (int i = 0; i < 80; i++) {
                    Node a, b;
                    do {
                        a = _nodes[PseudoRandom.Int32(_nodes.Count - 1)];
                        b = _nodes[PseudoRandom.Int32(_nodes.Count - 1)];
                    } while (a == b || a.IsConnectedTo(b));
                    Connect(a, b);
                }

                // Add group nodes. 
                for (int i = 0; i < 200; i++) {
                    Node node = new Node(colour);
                    Connect(node, _nodes[PseudoRandom.Int32(10)]);
                    Add(node);
                }

                // Add outlier nodes. 
                for (int i = 0; i < 200; i++) {
                    Node node = new Node(colour);
                    Connect(node, _nodes[PseudoRandom.Int32(_nodes.Count - 1)]);
                    Add(node);
                }

                // Connect more nodes. 
                for (int i = 0; i < 50; i++) {
                    Node a, b;
                    do {
                        a = _nodes[PseudoRandom.Int32(_nodes.Count - 1)];
                        b = _nodes[PseudoRandom.Int32(_nodes.Count - 1)];
                    } while (a == b || a.IsConnectedTo(b));
                    Connect(a, b);
                }

                // Give each node a random label. 
                foreach (Node node in _nodes)
                    if (node.Label == null)
                        node.Label = node.Location.Z.ToString();
            }
        }
    }
}
