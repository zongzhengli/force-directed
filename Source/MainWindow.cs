using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Lattice;

namespace ForceDirected {

    public partial class MainWindow : Form {

        /// <summary>
        /// The brush for drawing the info text. 
        /// </summary>
        private static readonly Brush InfoBrush = new SolidBrush(Color.FromArgb(50, Color.White));

        /// <summary>
        /// The font for drawing the info text.
        /// </summary>
        private static readonly Font InfoFont = new Font("Lucida Console", 8);

        /// <summary>
        /// The distance from the right border to align the info text. 
        /// </summary>
        private static readonly int InfoWidth = 160;

        /// <summary>
        /// The distance from the top border and between info text lines. 
        /// </summary>
        private static readonly int InfoHeight = 14;

        /// <summary>
        /// The distance from the top border of the topmost info text line. 
        /// </summary>
        private static readonly int InfoHeightInitial = -3;

        /// <summary>
        /// The multiplicative factor that gives the rate the update FPS converges. 
        /// </summary>
        private const double UpdateFpsEasing = 0.2;

        /// <summary>
        /// The multiplicative factor that gives the rate the update FPS converges. 
        /// </summary>
        private const double DrawFpsEasing = 0.2;

        /// <summary>
        /// The maximum update FPS displayed. 
        /// </summary>
        private const double UpdateFpsMax = 999.9;

        /// <summary>
        /// The maximum draw FPS displayed. 
        /// </summary>
        private const double DrawFpsMax = 999.9;

        /// <summary>
        /// The target number of milliseconds between model updates. 
        /// </summary>
        private const int UpdateInterval = 20;

        /// <summary>
        /// The target number of milliseconds between window drawing. 
        /// </summary>
        private const int DrawInterval = 33;

        /// <summary>
        /// The model of nodes and edges. 
        /// </summary>
        private WorldModel _model = new WorldModel();

        /// <summary>
        /// The timer used to measure the drawing FPS. 
        /// </summary>
        private Stopwatch _drawTimer = new Stopwatch();

        /// <summary>
        /// The update FPS counter. 
        /// </summary>
        private double _updateFps = 0;

        /// <summary>
        /// The drawing FPS counter. 
        /// </summary>
        private double _drawFps = 0;

        /// <summary>
        /// The second most recent mouse location. 
        /// </summary>
        private Point _previousMouseLocation;

        /// <summary>
        /// Whether the mouse is undergoing a drag gesture. 
        /// </summary>
        private bool _drag = false;

        /// <summary>
        /// Constructs and initializes the main window. 
        /// </summary>
        public MainWindow() {
            InitializeComponent();
            DoubleBuffered = true;
            _model.GenerateDemo();

            Paint += Draw;

            InitializeMouseEvents();
            StartDraw();
            StartUpdate();
        }

        /// <summary>
        /// Draws the window and model. 
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The paint event data.</param>
        private void Draw(object sender, PaintEventArgs e) {
            Graphics g = e.Graphics;

            // Draw model. 
            g.TranslateTransform(Width / 2, Height / 2);
            _model.Draw(g);
            g.ResetTransform();

            // Draw info text. 
            int x = Width - InfoWidth;
            int y = InfoHeightInitial;
            g.DrawString(String.Format("{0,-9}{1:#0.0}", "Model", _updateFps), InfoFont, InfoBrush, x, y += InfoHeight);
            g.DrawString(String.Format("{0,-9}{1:#0.0}", "Render", _drawFps), InfoFont, InfoBrush, x, y += InfoHeight);
            g.DrawString(String.Format("{0,-9}{1}", "Nodes", _model.NodeCount), InfoFont, InfoBrush, x, y += InfoHeight);
            g.DrawString(String.Format("{0,-9}{1}", "Edges", _model.EdgeCount), InfoFont, InfoBrush, x, y += InfoHeight);
            g.DrawString(String.Format("{0,-9}{1}", "Frames", _model.Frames), InfoFont, InfoBrush, x, y += InfoHeight);

            g.DrawString("ZONG ZHENG LI", InfoFont, InfoBrush, x, Height - 60);

            // Fps stuff. 
            _drawTimer.Stop();
            _drawFps += ((1000.0 / _drawTimer.Elapsed.TotalMilliseconds) - _drawFps) * DrawFpsEasing;
            _drawFps = Math.Min(_drawFps, DrawFpsMax);
            _drawTimer.Reset();
            _drawTimer.Start();
        }

        /// <summary>
        /// Initializes mouse behaviour. 
        /// </summary>
        private void InitializeMouseEvents() {

            // Initialize mouse down behaviour. 
            MouseDown += (sender, e) => {
                _previousMouseLocation = e.Location;
                _drag = true;

                _model.StopCamera();
            };

            // Initialize mouse up behaviour. 
            MouseUp += (sender, e) => {
                _drag = false;
            };

            // Initialize mouse move behaviour. 
            MouseMove += (sender, e) => {
                int dx = e.X - _previousMouseLocation.X;
                int dy = e.Y - _previousMouseLocation.Y;

                if (_drag)
                    RotationHelper.MouseDrag(_model.Rotate, dx, dy);

                _previousMouseLocation = e.Location;
            };

            // Initialize mouse wheel behaviour. 
            MouseWheel += (sender, e) => {
                _model.MoveCamera(e.Delta); ;
            };
        }

        /// <summary>
        /// Starts the draw thread. 
        /// </summary>
        private void StartDraw() {
            new Thread(new ThreadStart(() => {
                while (true) {
                    Invalidate();
                    Thread.Sleep(DrawInterval);
                }
            })) {
                IsBackground = true
            }.Start();
        }

        /// <summary>
        /// Starts the update thread. 
        /// </summary>
        private void StartUpdate() {
            new Thread(new ThreadStart(() => {
                Stopwatch timer = new Stopwatch();

                while (true) {

                    // Update the model. 
                    timer.Start();

                    if (!_drag)
                        _model.Update();

                    // Sleep for appropriate duration. 
                    int elapsed = (int)timer.ElapsedMilliseconds;
                    if (elapsed < UpdateInterval)
                        Thread.Sleep(UpdateInterval - elapsed);

                    // Fps stuff. 
                    timer.Stop();
                    _updateFps += ((1000.0 / timer.Elapsed.TotalMilliseconds) - _updateFps) * UpdateFpsEasing;
                    _updateFps = Math.Min(_updateFps, UpdateFpsMax);
                    timer.Reset();
                }
            })) {
                IsBackground = true
            }.Start();
        }
    }
}
